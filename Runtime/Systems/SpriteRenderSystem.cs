using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace SpriteRendering 
{
    [UpdateInGroup(typeof( PresentationSystemGroup ), OrderLast = true)] 
    public class SpriteRenderSystem : SystemBase
    {

        private InstancedRenderManager m_instancedRenderManager;

        protected override void OnCreate( ) { m_instancedRenderManager = new InstancedRenderManager( EntityManager, this ); }

        protected override void OnUpdate( )
        {
            var spriteReference = GetEntityQuery( typeof( RenderSprite ), typeof( SpriteRenderData ) );

            var archetypeChunkArray = spriteReference.CreateArchetypeChunkArray(Allocator.TempJob);
        
            CacheMeshBatchRendererGroup( archetypeChunkArray );

            archetypeChunkArray.Dispose( );
        }

        protected override void OnDestroy( ) { m_instancedRenderManager.Dispose( ); }
    
        struct SpriteRenderBatch
        {
            public int ChunkStart;
            public int ChunkEnd;
        
            public int Order;
        }

        struct RenderBatchComparer : IComparer<SpriteRenderBatch>
        {
            public int Compare( SpriteRenderBatch x, SpriteRenderBatch y ) => x.Order.CompareTo( y.Order );
        }
    
        public void CacheMeshBatchRendererGroup( NativeArray<ArchetypeChunk> chunks )
        {
            m_instancedRenderManager.PrepareAddBatches( );

            var renderMeshType = GetSharedComponentTypeHandle<RenderSprite>( );
        
            Profiler.BeginSample( "Sort Shared Renderers" );
        
            var chunkRenderer = new NativeArray<int>( chunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            var sortedChunks = new NativeArraySharedInt( chunkRenderer, Allocator.TempJob );

            var gatherChunkRenderersJob = new GatherChunkRenderers
            {
                Chunks = chunks,
                RendererType = renderMeshType,
                ChunkRenderer = chunkRenderer
            };
            
            var gatherChunkRenderersJobHandle = gatherChunkRenderersJob.Schedule( chunks.Length, 64 );
            var sortedChunksJobHandle = sortedChunks.Schedule( gatherChunkRenderersJobHandle );

            sortedChunksJobHandle.Complete( );
            Profiler.EndSample( );

        
            var sharedRenderCount = sortedChunks.SharedValueCount;
            var sharedRendererCounts = sortedChunks.GetSharedValueIndexCountArray( );
            var sortedChunkIndices = sortedChunks.GetSortedIndices( );
        
            var renderBatches = new NativeArray<SpriteRenderBatch>( sharedRenderCount, Allocator.Temp );

            Profiler.BeginSample("Sort Renderers by Layer");
            {

                var sortedChunkIndex = 0;
                for ( int i = 0; i < sharedRenderCount; i++ )
                {
                    var renderSpriteIdx = chunks[sortedChunkIndices[sortedChunkIndex]].GetSharedComponentIndex( renderMeshType );
                    var sortLayer = EntityManager.GetSharedComponentData<RenderSprite>( renderSpriteIdx ).sortOrder;
                
                    renderBatches[i] = new SpriteRenderBatch( )
                    {
                        ChunkStart = sortedChunkIndex,
                        ChunkEnd = sortedChunkIndex + sharedRendererCounts[i],
                        Order = sortLayer
                    };

                    sortedChunkIndex = renderBatches[i].ChunkEnd;
                }

                renderBatches.Sort( default( RenderBatchComparer ) );
            }

            Profiler.EndSample();
        
            Profiler.BeginSample( "Add New Batches" );
            {
                for ( int i = 0; i < renderBatches.Length; ++i )
                {
                    var renderBatch = renderBatches[i];
                    var sortedChunkIdx = renderBatch.ChunkStart;
                    var endSortedChunkIndex = renderBatch.ChunkEnd;
                
                    while ( sortedChunkIdx < endSortedChunkIndex )
                    {
                        var chunkIndex = sortedChunkIndices[sortedChunkIdx];
                        var chunk = chunks[chunkIndex];
                        var rendererSharedComponentIndex = chunk.GetSharedComponentIndex( renderMeshType );

                        var remainingEntitySlots = BatchCapacity;
                        int instanceCount = chunk.Count;
                        int startSortedIndex = sortedChunkIdx;
                        int batchChunkCount = 1;

                        remainingEntitySlots -= chunk.Count;
                        sortedChunkIdx++;

                        while ( remainingEntitySlots > 0 )
                        {
                            if ( sortedChunkIdx >= endSortedChunkIndex ) break;

                            var nextChunkIndex = sortedChunkIndices[sortedChunkIdx];
                            var nextChunk = chunks[nextChunkIndex];
                            if ( nextChunk.Count > remainingEntitySlots ) break;

                            remainingEntitySlots -= nextChunk.Count;
                            instanceCount += nextChunk.Count;
                            batchChunkCount++;
                            sortedChunkIdx++;
                        }

                    
                        m_instancedRenderManager.AddDynamicBatch( rendererSharedComponentIndex, instanceCount, chunks, startSortedIndex, batchChunkCount );
                    }
                }
            }
            Profiler.EndSample( );

            chunkRenderer.Dispose( );
            sortedChunks.Dispose( );
        }

        public const int BatchCapacity = 1023;
    }
}