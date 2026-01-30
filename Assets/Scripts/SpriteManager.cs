using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SpriteManager", menuName = "CandyBlast/Sprite Manager")]
public class SpriteManager : ScriptableObject
{
    [System.Serializable]
    public class ColorSprites
    {
        public BlockType blockType;
        public Sprite defaultSprite;
        public Sprite variantA;
        public Sprite variantB;
        public Sprite variantC;
    }

    [Title("Block Sprites")]
    [SerializeField] private List<ColorSprites> colorSprites = new List<ColorSprites>();

    private Dictionary<BlockType, ColorSprites> spriteCache;

    private void OnEnable()
    {
        InitializeCache();
    }

    private void InitializeCache()
    {
        spriteCache = new Dictionary<BlockType, ColorSprites>();
        foreach (var colorSprite in colorSprites)
        {
            if (!spriteCache.ContainsKey(colorSprite.blockType))
            {
                spriteCache[colorSprite.blockType] = colorSprite;
            }
        }
    }

    public Sprite GetSprite(BlockType type, IconVariant variant)
    {
        if (spriteCache == null || spriteCache.Count == 0)
            InitializeCache();

        if (!spriteCache.TryGetValue(type, out ColorSprites sprites))
        {
            Debug.LogWarning($"No sprites found for block type: {type}");
            return null;
        }

        return variant switch
        {
            IconVariant.Default => sprites.defaultSprite,
            IconVariant.A => sprites.variantA,
            IconVariant.B => sprites.variantB,
            IconVariant.C => sprites.variantC,
            _ => sprites.defaultSprite
        };
    }

    public Sprite GetDefaultSprite(BlockType type)
    {
        return GetSprite(type, IconVariant.Default);
    }

    public IconVariant GetVariantForGroupSize(int groupSize, int thresholdA, int thresholdB, int thresholdC)
    {
        if (groupSize > thresholdC)
            return IconVariant.C;
        if (groupSize > thresholdB)
            return IconVariant.B;
        if (groupSize > thresholdA)
            return IconVariant.A;

        return IconVariant.Default;
    }

#if UNITY_EDITOR
    [Button("Auto-Load Sprites from Assets")]
    private void AutoLoadSprites()
    {
        colorSprites.Clear();


        string[] colors = { "Blue", "Green", "Pink", "Purple", "Red", "Yellow" };

        foreach (string color in colors)
        {
            ColorSprites sprites = new ColorSprites
            {
                blockType = (BlockType)System.Enum.Parse(typeof(BlockType), color)
            };


            sprites.defaultSprite = Resources.Load<Sprite>($"Textures/{color}_Default");
            sprites.variantA = Resources.Load<Sprite>($"Textures/{color}_A");
            sprites.variantB = Resources.Load<Sprite>($"Textures/{color}_B");
            sprites.variantC = Resources.Load<Sprite>($"Textures/{color}_C");

            colorSprites.Add(sprites);
        }

        UnityEditor.EditorUtility.SetDirty(this);
        // Debug.Log("Sprites auto-loaded successfully!");
    }
#endif
}
