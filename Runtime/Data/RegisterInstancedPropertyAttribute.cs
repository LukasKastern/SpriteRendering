using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SpriteRendering {
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
}