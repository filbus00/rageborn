using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Puts a marker at each corpse the character left in this level. Walking onto a marker picks its gear back up.
    /// A level that holds no corpse is left alone, and corpses never expire.
    /// </summary>
    public class CorpseSpawner : MonoBehaviour
    {
        [Tooltip("Placeholder marker art. Its pivot is at the bottom.")]
        [SerializeField] Sprite corpseSprite;

        void Start()
        {
            var levelId = gameObject.scene.name;
            var corpses = GameSession.Current.Corpses;
            for (var i = 0; i < corpses.Count; i++)
                if (corpses[i].LevelId == levelId)
                    CreateMarker(corpses[i]);
        }

        void CreateMarker(Corpse corpse)
        {
            var go = new GameObject("Corpse", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(CorpseMarker));
            var world = IsoMath.GroundToWorld(corpse.GroundPosition);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(world.x, world.y, 0f);

            var layer = LayerMask.NameToLayer(GameLayers.Interactable);
            if (layer >= 0)
                go.layer = layer;

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = corpseSprite;
            spriteRenderer.sortingLayerName = GameSortingLayers.Entities;
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.6f;

            go.GetComponent<CorpseMarker>().Init(corpse);
        }
    }
}
