#if SUPER_TILEMAP_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreativeSpore.SuperTilemapEditor;

namespace Bird.Light2D
{
	[ExecuteInEditMode]
	public class LightColliderSTM : MonoBehaviour
	{
		public void OnEnable()
		{
			var tilemap = GetComponent<STETilemap>();
			if (tilemap != null)
			{
				tilemap.OnTileChanged += OnTileChanged;
				InitChunks();
			}
		}
		public void OnDisable()
		{
			var tilemap = GetComponent<STETilemap>();
			if (tilemap != null)
			{
				tilemap.OnTileChanged -= OnTileChanged;
			}
		}
		void InitChunks()
		{
			var chunks = GetComponentsInChildren<TilemapChunk>();
			foreach (var chunk in chunks)
			{
				UpdateCollider(chunk.gameObject);
			}
		}
		LightCollider UpdateCollider(GameObject chunk)
		{
			//Get/Add Collider
			var collider = chunk.GetComponent<LightCollider>();
			if (collider == null)
				collider = chunk.AddComponent<LightCollider>();
			//chunk.layer = LightingFeature.inst.colliderLayerMask.ToLayerInt();
			//chunk.hideFlags = HideFlags.None;
			return collider;
		}
		void OnTileChanged(STETilemap tilemap, int gridX, int gridY, uint tileData)
		{
			//Find chunk
			var chunk = FindChunk(gridX, gridY);
			if(chunk != null)
			{
				var collider = UpdateCollider(chunk);
				collider.DirtyCollider();
			}
		}
		GameObject FindChunk(int gridX, int gridY)
		{
			int chunkX = (gridX < 0 ? (gridX + 1 - STETilemap.k_chunkSize) : gridX) / STETilemap.k_chunkSize;
			int chunkY = (gridY < 0 ? (gridY + 1 - STETilemap.k_chunkSize) : gridY) / STETilemap.k_chunkSize;
			string chunkName = chunkX + "_" + chunkY;
			var result = gameObject.transform.Find(chunkName);
			return result != null ? result.gameObject : null;
		}

	}
}

#endif