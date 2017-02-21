using SpaceEngine.Core.Terrain;
using SpaceEngine.Core.Tile.Cache;
using SpaceEngine.Core.Tile.Layer;
using SpaceEngine.Core.Tile.Samplers;
using SpaceEngine.Core.Tile.Storage;
using SpaceEngine.Core.Tile.Tasks;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace SpaceEngine.Core.Tile.Producer
{
    /// <summary>
    /// An abstract producer of tiles. A TileProducer must be inherited from and overide the <see cref="DoCreateTile"/> function to create the tiles data.
    /// Note that several TileProducer can share the same TileCache, and hence the same TileStorage.
    /// </summary>
    [RequireComponent(typeof(TileSampler))]
    public abstract class TileProducer : Node<TileProducer>
    {
        /// <summary>
        /// The tile cache game object that stores the tiles produced by this producer.
        /// </summary>
        [SerializeField]
        GameObject CacheGameObject;

        public TileCache Cache { get; private set; }

        /// <summary>
        /// The name of the uniforms this producers data will be bound if used in a shader.
        /// </summary>
        [SerializeField]
        string Name;

        //Does this producer use the gpu 
        [SerializeField]
        bool isGPUProducer = true;
        public bool IsGPUProducer { get { return isGPUProducer; } protected set { isGPUProducer = value; } }

        /// <summary>
        /// Layers, that may modify the tile created by this producer and are optional.
        /// </summary>
        public TileLayer[] Layers { get; protected set; }

        /// <summary>
        /// The tile sampler associated with this producer.
        /// </summary>
        public TileSampler Sampler { get; protected set; }

        /// <summary>
        /// The id of this producer. This id is local to the TileCache used by this producer, and is used to distinguish all the producers that use this cache.
        /// </summary>
        public int ID { get; protected set; }

        public TerrainNode TerrainNode { get { return Sampler.TerrainNode; } }

        #region Node

        protected override void InitNode()
        {
            InitCache();

            // Get any layers attached to same GameObject. May have 0 to many attached.
            Layers = GetComponents<TileLayer>();

            // Get the samplers attached to GameObject. Must have one sampler attahed.
            Sampler = GetComponent<TileSampler>();
        }

        protected override void UpdateNode()
        {

        }


        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion

        /// <summary>
        /// It's posible that a producer will have a call to get it's cache before it's start fuction has been called. 
        /// Call InitCache in the start and get functions to ensure that the cache is always init before being returned.
        /// </summary>
        public void InitCache()
        {
            if (Cache == null)
            {
                Cache = CacheGameObject.GetComponent<TileCache>();
                ID = Cache.NextProducerId;
                Cache.InsertProducer(ID, this);
            }
        }

        public string GetName()
        {
            return Name;
        }

        public int GetTileSize(int i)
        {
            // TODO : CORE FIX.
            InitCache();

            return Cache.GetStorage(i).TileSize;
        }

        public int GetTileSizeMinBorder(int i)
        {
            return GetTileSize(i) - GetBorder() * 2;
        }

        /// <summary>
        /// Tiles made of rasster data may have a border that contains the value of the neighboring pixels of the tile. 
        /// For instance if the tile size (returned by TileStorage.GetTileSize) is 196, and if the tile border is 2, this means that the actual tile data is 192x192 pixels, 
        /// with a 2 pixel border that contains the value of the neighboring pixels. 
        /// Using a border introduces data redundancy but is usefull to get the value of the neighboring pixels of a tile without needing to load the neighboring tiles.
        /// </summary>
        /// <returns>Returns the size in pixels of the border of each tile.</returns>
        public virtual int GetBorder()
        {
            return 0;
        }

        /// <summary>
        /// Check if this producer can produce the given tile.
        /// </summary>
        /// <param name="level">The tile's quadtree level.</param>
        /// <param name="tx">The tile's quadtree X coordinate.</param>
        /// <param name="ty">The tile's quadtree Y coordinate.</param>
        /// <returns>Returns 'True' if this producer can produce the given tile.</returns>
        public virtual bool HasTile(int level, int tx, int ty)
        {
            return true;
        }

        /// <summary>
        /// Check if this producer can produce the children of the given tile.
        /// </summary>
        /// <param name="level">The tile's quadtree level.</param>
        /// <param name="tx">The tile's quadtree X coordinate.</param>
        /// <param name="ty">The tile's quadtree Y coordinate.</param>
        /// <returns>Returns 'True' if this producer can produce the children of the given tile.</returns>
        public virtual bool HasChildren(int level, int tx, int ty)
        {
            return HasTile(level + 1, 2 * tx, 2 * ty);
        }

        /// <summary>
        /// Decrements the number of users of this tile by one. If this number becomes 0 the tile is marked as unused, and so can be evicted from the cache at any moment.
        /// </summary>
        /// <param name="tile">Tile to put.</param>
        public virtual void PutTile(Tile tile)
        {
            Cache.PutTile(tile);
        }

        /// <summary>
        /// Returns the requested tile, creating it if necessary.If the tile is
        /// currently in use it is returned directly.If it is in cache but unused,
        /// it marked as used and returned.Otherwise a new tile is created, marked
        /// as used and returned.In all cases the number of users of this tile is
        /// incremented by one.
        /// </summary>
        /// <param name="level">The tile's quadtree level.</param>
        /// <param name="tx">The tile's quadtree X coordinate.</param>
        /// <param name="ty">The tile's quadtree Y coordinate.</param>
        /// <returns>Returns the requested tile.</returns>
        public virtual Tile GetTile(int level, int tx, int ty)
        {
            return Cache.GetTile(ID, level, tx, ty);
        }

        /// <summary>
        /// Looks for a tile in the TileCache of this TileProducer.
        /// </summary>
        /// <param name="level">The tile's quadtree level.</param>
        /// <param name="tx">The tile's quadtree X coordinate.</param>
        /// <param name="ty">The tile's quadtree Y coordinate.</param>
        /// <param name="includeUnusedCache">Include unused tiles in the search, or not?</param>
        /// <param name="done">Check that tile's creation task is done?</param>
        /// <returns>
        /// Returns the requsted tile, or null if it's not in the TileCache or if it's not ready. This method doesn't change the number of users of the returned tile.
        /// </returns>
        public virtual Tile FindTile(int level, int tx, int ty, bool includeUnusedCache, bool done)
        {
            var tile = Cache.FindTile(ID, level, tx, ty, includeUnusedCache);

            if (done && tile != null && !tile.Task.IsDone)
            {
                tile = null;
            }

            return tile;
        }

        /// <summary>
        /// Creates a Task to produce the data of the given tile.
        /// </summary>
        /// <param name="level">The tile's quadtree level.</param>
        /// <param name="tx">The tile's quadtree X coordinate.</param>
        /// <param name="ty">The tile's quadtree Y coordinate.</param>
        /// <param name="slot">Slot, where the crated tile data must be stored.</param>
        public virtual CreateTileTask CreateTile(int level, int tx, int ty, List<TileStorage.Slot> slot)
        {
            return new CreateTileTask(this, level, tx, ty, slot);
        }

        /// <summary>
        /// Creates the given tile. If this task requires tiles produced by other. The default implementation of this method calls DoCreateTile on each Layer of this producer.
        /// </summary>
        /// <param name="level">The tile's quadtree level.</param>
        /// <param name="tx">The tile's quadtree X coordinate.</param>
        /// <param name="ty">The tile's quadtree Y coordinate.</param>
        /// <param name="slot">Slot, where the crated tile data must be stored.</param>
        public virtual void DoCreateTile(int level, int tx, int ty, List<TileStorage.Slot> slot)
        {
            if (Layers == null) return;

            foreach (var layer in Layers)
            {
                layer.DoCreateTile(level, tx, ty, slot);
            }
        }

        [Obsolete("Not currently used and maybe not working correctly.")]
        public Vector4 GetGpuTileCoords(int level, int tx, int ty, ref Tile tile)
        {
            var s = Cache.GetStorage(0).TileSize;
            var b = GetBorder();

            var dx = 0.0f;
            var dy = 0.0f;
            var dd = 1.0f;
            var ds0 = ((float)s / 2.0f) * 2.0f - 2.0f * (float)b;
            var ds = ds0;

            while (!HasTile(level, tx, ty))
            {
                dx += (tx % 2) * dd;
                dy += (ty % 2) * dd;
                dd *= 2;
                ds /= 2;
                level -= 1;
                tx /= 2;
                ty /= 2;

                if (level < 0)
                {
                    Debug.Log("Proland::TileProducer::GetGpuTileCoords - invalid level (A)");
                    Debug.Break();
                }
            }

            var t = tile == null ? FindTile(level, tx, ty, true, true) : null;

            while (tile == null ? t == null : level != tile.Level)
            {
                dx += (tx % 2) * dd;
                dy += (ty % 2) * dd;
                dd *= 2;
                ds /= 2;
                level -= 1;
                tx /= 2;
                ty /= 2;

                if (level < 0)
                {
                    Debug.Log("Proland::TileProducer::GetGpuTileCoords - invalid level (B)");
                    Debug.Break();
                }

                t = tile == null ? FindTile(level, tx, ty, true, true) : null;
            }

            dx = dx * ((s / 2) * 2 - 2 * b) / dd;
            dy = dy * ((s / 2) * 2 - 2 * b) / dd;

            if (tile == null)
            {
                tile = t;
            }
            else
            {
                t = tile;
            }

            var w = (float)s;

            if (s % 2 == 0)
            {
                return new Vector4((dx + b) / w, (dy + b) / w, 0.0f, ds / w);
            }
            else
            {
                return new Vector4((dx + b + 0.5f) / w, (dy + b + 0.5f) / w, 0.0f, ds / w);
            }
        }
    }
}