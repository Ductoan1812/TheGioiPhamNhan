using System;
using UnityEngine;
using Xianxia.Items;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace Xianxia.Player
{
    // Quản lý việc gán sprite cho các renderer dựa trên slot và id item
    public class PlayerRenderer : MonoBehaviour
    {
        [System.Serializable]
        public class SlotRendererGroup
        { // vd: "armor"
            public Renderer renderers;
            public string spriteNames;
            public int spriteIndexes;
        }

        public SlotRendererGroup[] weapon_r;
        public SlotRendererGroup[] weapon_l;
        public SlotRendererGroup[] armor;
        public SlotRendererGroup[] cloth;
        public SlotRendererGroup[] helmet;
        public SlotRendererGroup[] foot;
        public SlotRendererGroup[] body;
        public SlotRendererGroup[] pet;
        public SlotRendererGroup[] back;

        /// <summary>
        /// Gán sprite cho tất cả renderer trong một nhóm slot, truyền vào mảng group và addressTexture
        /// </summary>

        public async System.Threading.Tasks.Task SetSlotSprites(SlotRendererGroup[] group, string addressTexture)
        {
            if (group == null || group.Length == 0) return;
            foreach (var slot in group)
            {
                if (slot == null || slot.renderers == null) continue;
                SpriteRenderer sr = slot.renderers as SpriteRenderer;

                if (string.IsNullOrEmpty(slot.spriteNames))
                {
                    var sprites = await ItemAssets.LoadAllSpritesAsync(addressTexture);
                    if (sprites != null && sprites.Length > 0 && sr != null)
                    {
                        sr.sprite = sprites[slot.spriteIndexes];
                    }
                    else
                    {
                        Texture2D sp = await ItemAssets.LoadTextureAsync(addressTexture);
                        if (sr != null)
                            sr.sprite = Sprite.Create(sp, new Rect(0, 0, sp.width, sp.height), new Vector2(0.5f, 0.5f));
                        else
                            slot.renderers.material.mainTexture = sp;
                    }
                }
                else
                {
                    Sprite sp = await ItemAssets.LoadIconSpriteAsync(addressTexture + "[" + slot.spriteNames + "]");
                    if (sr != null) sr.sprite = sp;
                }
            }
        }

        // Xóa hiển thị của một nhóm slot (khi unequip)
        public void ClearSlotSprites(SlotRendererGroup[] group)
        {
            if (group == null || group.Length == 0) return;
            foreach (var slot in group)
            {
                if (slot == null || slot.renderers == null) continue;
                var sr = slot.renderers as SpriteRenderer;
                if (sr != null)
                {
                    sr.sprite = null;
                }
                else if (slot.renderers.material != null)
                {
                    slot.renderers.material.mainTexture = null;
                }
            }
        }
    }
}      

