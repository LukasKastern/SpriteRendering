using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using SpriteRendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace SpriteRendering 
{
    public class InstancedRenderManager
    {
        private readonly EntityManager m_entityManager;
        private readonly BatchRendererGroup m_rendererGroup;
        private readonly SpriteRenderSystem m_spriteRenderSystem;
        private readonly List<int> m_dynamicBatches = new List<int>( 128 );
        private readonly List<MaterialPropertyBlock> m_MaterialPropertyBlocks; //Per batch data
        private readonly Vector4[] m_vectorCopyBuffer = new Vector4[1023];
        private readonly Matrix4x4[] m_matrixBuffer = new Matrix4x4[1023];

        private readonly float[] m_floatCopyBuffer = new float[1023];

            
        private int m_instances = 0;
        private int m_batchCount = 0;
        
        public InstancedRenderManager( EntityManager entityManager, SpriteRenderSystem spriteRenderSystem )
        {
            m_spriteRenderSystem = spriteRenderSystem;
            m_entityManager = entityManager;
            m_rendererGroup = new BatchRendererGroup(OnCull);
            m_MaterialPropertyBlocks = new List<MaterialPropertyBlock>( 128 );

            SetupInstanceTypeHandles( );
        }

        private void SetupInstanceTypeHandles( )
        {
            RegisterInstancedPropertyAttribute.Initialize( );

            var float4Types = RegisterInstancedPropertyAttribute.Float4Properties;
            m_float4Handles.Capacity = float4Types.Count;
            
            for ( int i = 0; i < float4Types.Count; ++i )
            {
                var instancedProperty = float4Types[i];

                m_float4Handles.Add( new Float4Property( )
                {
                    PropertyId = instancedProperty.ShaderPropertyId,
                    ComponentType = instancedProperty.ComponentType,
                    DefaultValue = instancedProperty.DefaultValue
                } );
            }
            
            var floatTypes = RegisterInstancedPropertyAttribute.FloatProperties;
            m_floatHandles.Capacity = floatTypes.Count;
            
            for ( int i = 0; i < floatTypes.Count; ++i )
            {
                var instancedProperty = floatTypes[i];

                m_floatHandles.Add( new FloatProperty( )
                {
                    PropertyId = instancedProperty.ShaderPropertyId,
                    ComponentType = instancedProperty.ComponentType,
                    DefaultValue = instancedProperty.DefaultValue 
                } );
            }
        }
        
        
        public void Dispose( ) { m_rendererGroup.Dispose( ); }

        [BurstCompile]
        struct DoNotCullAnythingJob : IJob
        {
            public BatchCullingContext Context;
        
            public void Execute( )
            {
            
                for (var batchIndex= 0;  batchIndex<Context.batchVisibility.Length ; ++batchIndex)
                {
                    var batchVisibility = Context.batchVisibility[batchIndex];
 
                    for (var i = 0; i < batchVisibility.instancesCount; ++i)
                    {
                        Context.visibleIndices[batchVisibility.offset + i] = i;
                    }
 
                    batchVisibility.visibleCount = batchVisibility.instancesCount;
                    Context.batchVisibility[batchIndex] = batchVisibility;
                }
            }
        }


        private JobHandle m_previousCullingJob;
        private JobHandle OnCull( BatchRendererGroup rendererGroup, BatchCullingContext cullingContext )
        {
            m_previousCullingJob.Complete( );
        
            m_previousCullingJob = new DoNotCullAnythingJob( )
            {
                Context = cullingContext
            }.Schedule( );

            return m_previousCullingJob;
        }
        
        public unsafe void AddDynamicBatch( int renderSharedComponentIndex, int instanceCount, NativeArray<ArchetypeChunk> chunks, int startChunk, int chunkCount )
        {
            m_instances += instanceCount;
            m_batchCount++;
        
            var spriteSheetData = m_entityManager.GetSharedComponentData<RenderSprite>( renderSharedComponentIndex );
            var bigBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(16738.0f, 16738.0f, 16738.0f));
        
            var mesh = spriteSheetData.mesh;
            var material = spriteSheetData.material;

            if ( mesh == null || material == null || instanceCount == 0 ) return;

//            Debug.Log($"Drawing  Batch: {m_batchCount}: {material.shader.name}");
            
            while (  m_MaterialPropertyBlocks.Count < m_batchCount )
                m_MaterialPropertyBlocks.Add( new MaterialPropertyBlock( ) );

            var propertyBlock = m_MaterialPropertyBlocks[m_batchCount - 1];

            

            Profiler.BeginSample( "Queueing Batch to add" );
            
            var batchIdx = m_rendererGroup.AddBatch(
            mesh,
            0,
            material,
            0,
            ShadowCastingMode.Off,
            false,
            false,
            bigBounds,
            instanceCount,
            propertyBlock,
            null );
            Profiler.EndSample( );
      
            m_dynamicBatches.Add( batchIdx );
        

            Profiler.BeginSample( "Copy Instance Data" );
            
            var propertyChunkPointers = new NativeList<PropertyChunkPointer>( chunkCount, Allocator.Temp );

            for ( int i = 0; i < m_floatHandles.Count; ++i )
            {
                var floatHandle = m_floatHandles[i];

                GetPropertyChunkPointers<float>( propertyChunkPointers, chunks, startChunk, chunkCount, m_entityManager.GetDynamicComponentTypeHandle(floatHandle.ComponentType), 4 );
                
                fixed ( float* cpy = m_floatCopyBuffer )
                {
                    if ( propertyChunkPointers.Length > 0 )
                    {
                        PopulateCopyBuffer( propertyChunkPointers, cpy, floatHandle.DefaultValue, 4, instanceCount );
                        propertyBlock.SetFloatArray( floatHandle.PropertyId, m_floatCopyBuffer );
                    }
                }
            }
            
            for ( int i = 0; i < m_float4Handles.Count; ++i )
            {
                var float4Handle = m_float4Handles[i];

                GetPropertyChunkPointers<float4>( propertyChunkPointers, chunks, startChunk, chunkCount, m_entityManager.GetDynamicComponentTypeHandle(float4Handle.ComponentType), 16 );
                
                fixed ( Vector4* cpy = m_vectorCopyBuffer )
                {
                    if ( propertyChunkPointers.Length > 0 )
                    {
                        PopulateCopyBuffer( propertyChunkPointers, cpy, float4Handle.DefaultValue, 16, instanceCount );
                        
                        propertyBlock.SetVectorArray( float4Handle.PropertyId, m_vectorCopyBuffer );
                    }
                }
            }

            Profiler.EndSample( );
        }

        private static unsafe void PopulateCopyBuffer<T>( NativeArray<PropertyChunkPointer> chunkPointers, void* copyBuffer, T defaultValue, int typeSize, int instanceCount ) where T : unmanaged
        {
            //Initialize copy buffer with the default value of this property
            UnsafeUtility.MemCpyReplicate( copyBuffer, &defaultValue, 4, instanceCount );

            //After that we set the per chunk data
            for ( int j = 0; j < chunkPointers.Length; ++j )
            {
                var chunkPtr = chunkPointers[j];
                
                UnsafeUtility.MemCpy( ((byte*) copyBuffer) + chunkPtr.StartInCopyBuffer * typeSize, chunkPtr.PointerToDataInChunk, chunkPtr.ChunkLength * typeSize );
            }
        }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetPropertyChunkPointers<T>( NativeList<PropertyChunkPointer> pointers, NativeArray<ArchetypeChunk> chunks, int chunkStartOffset, int chunkCount, DynamicComponentTypeHandle componentTypeHandle, int typeSize ) where T : unmanaged
        {
            pointers.Clear( );

            int offset = 0;
            
            for ( int j = chunkStartOffset; j < chunkStartOffset + chunkCount; ++j )
            {
                var chunk = chunks[j];

                if ( chunk.Has( componentTypeHandle ) )
                {
                    pointers.Add( new PropertyChunkPointer( )
                    {
                        StartInCopyBuffer = offset,
                        ChunkLength = chunk.Count,
                        PointerToDataInChunk = chunk.GetDynamicComponentDataArrayReinterpret<T>( componentTypeHandle, typeSize ).GetUnsafeReadOnlyPtr( )
                    } );
                }

                offset += chunk.Count;
            }
        }

        struct PropertyChunkPointer
        {
            public unsafe void* PointerToDataInChunk;
            public int ChunkLength;
            public int StartInCopyBuffer;
        }
        
        
        public void PrepareAddBatches( )
        {
            //   Debug.Log($"Dew {m_instances} instances in {m_batchCount} batches");
        
            m_instances = 0;
            m_batchCount = 0;
        
            for ( int i = m_dynamicBatches.Count - 1; i > -1; i-- )
                m_rendererGroup.RemoveBatch( m_dynamicBatches[i] );
        
            m_dynamicBatches.Clear( );
        }

        struct FloatProperty
        {
            public int PropertyId;
            public float DefaultValue;
            public ComponentType ComponentType;
        }

        struct Float4Property
        {
            public int PropertyId;
            public float4 DefaultValue;
            public ComponentType ComponentType;
        }
        
        private List<Float4Property> m_float4Handles = new List<Float4Property>();

        private List<FloatProperty> m_floatHandles = new List<FloatProperty>();
    }
}

internal struct PropertyFloat4
{
    public float4 DefaultValue;
    public int ShaderPropertyId;
    public ComponentType ComponentType;
}

internal struct PropertyFloat
{
    public float DefaultValue;
    public int ShaderPropertyId;
    public ComponentType ComponentType;
}

[AttributeUsage( AttributeTargets.Struct )]
public class RegisterInstancedPropertyAttribute : Attribute
{
    internal static List<PropertyFloat4> Float4Properties;
    internal static List<PropertyFloat> FloatProperties;

    private InstancedPropertyType m_propertyType;
    private string m_shaderPropertyName;
    private object m_defaultValue;
    
    public RegisterInstancedPropertyAttribute( string shaderPropertyName, float x, float y, float z, float w )
    {
        m_defaultValue = new float4(x, y, z, w);
        m_shaderPropertyName = shaderPropertyName;
        m_propertyType = InstancedPropertyType.Float4;
    }
    
    public RegisterInstancedPropertyAttribute( string shaderPropertyName, float defaultValue )
    {
        m_defaultValue = defaultValue;
        m_shaderPropertyName = shaderPropertyName;
        m_propertyType = InstancedPropertyType.Float;
    }

    
    public static void Initialize( )
    {
        if ( Float4Properties != null ) return;

        Float4Properties = new List<PropertyFloat4>( );
        FloatProperties = new List<PropertyFloat>();
        
        TypeManager.Initialize( );
        
        foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies( ) )
        {
            if ( !IsAssemblyReferencingSpriteRendering( assembly ) ) continue;

            foreach ( var type in assembly.GetTypes() )
            {
                var instancedPropertyAttribute = type.GetCustomAttribute<RegisterInstancedPropertyAttribute>( );

                if ( instancedPropertyAttribute == null ) continue;

                var propertyType = instancedPropertyAttribute.m_propertyType;

                if ( propertyType == InstancedPropertyType.Float )
                {
                    var size = UnsafeUtility.SizeOf( type );

                    if ( size != 4 )
                    {
                        Debug.LogError( $"Component Type {type.Name} needs to have a size of 4 bytes but is {size}" );
                        continue;
                    }

                    FloatProperties.Add( new PropertyFloat()
                    {
                        ComponentType = new ComponentType( type ),
                        ShaderPropertyId = Shader.PropertyToID( instancedPropertyAttribute.m_shaderPropertyName ),
                        DefaultValue = (float)instancedPropertyAttribute.m_defaultValue
                    });
                    
                }
                
                else if ( propertyType == InstancedPropertyType.Float4 )
                {
                    var size = UnsafeUtility.SizeOf( type );

                    if ( size != 16 )
                    {
                        Debug.LogError( $"Component Type {type.Name} needs to have a size of 16 bytes but is {size}" );
                        continue;
                    }

                    Float4Properties.Add( new PropertyFloat4()
                    {
                        ComponentType = new ComponentType( type ),
                        ShaderPropertyId = Shader.PropertyToID( instancedPropertyAttribute.m_shaderPropertyName ),
                        DefaultValue = (float4) instancedPropertyAttribute.m_defaultValue
                    });
                }
            }
        }
    }

    private static bool IsAssemblyReferencingSpriteRendering(Assembly assembly)
    {
        const string assemblyName = "SpriteRendering";
        if (assembly.GetName().Name.Contains(assemblyName))
            return true;

        var referencedAssemblies = assembly.GetReferencedAssemblies();
        foreach (var referenced in referencedAssemblies)
            if (referenced.Name.Contains(assemblyName))
                return true;
        return false;
    }

}