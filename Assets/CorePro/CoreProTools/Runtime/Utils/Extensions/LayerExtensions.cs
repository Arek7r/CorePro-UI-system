using UnityEngine;

namespace CorePro.Utils.Extensions
{
    public static class LayerExtensions
    {
        
        /// <summary>
        /// Returns a new LayerMask with the added layers.
        /// </summary>
        public static LayerMask Add(this LayerMask mask, LayerMask toAdd)
        {
            return mask.value | toAdd.value;
        }

        /// <summary>
        /// Returns a new LayerMask with the specified layers removed.
        /// </summary>
        public static LayerMask Remove(this LayerMask mask, LayerMask toRemove)
        {
            return mask.value & ~toRemove.value;
        }
        
        /// <summary>
        /// Gets the first active layer index from a LayerMask.
        /// </summary>
        public static int ToIndex(this LayerMask mask)
        {
            int value = mask.value;
            if (value == 0) return 0;
            for (int i = 0; i < 32; i++)
            {
                if ((value & (1 << i)) != 0) return i;
            }
            return 0;
        }

        /// <summary>
        /// Sets the GameObject's layer using a LayerMask (converts mask to index automatically).
        /// </summary>
        public static void SetLayer(this GameObject go, LayerMask mask)
        {
            go.layer = mask.ToIndex();
        }

        /// <summary>
        /// Extension for Transform to set layer on the whole hierarchy.
        /// </summary>
        public static void SetLayerRecursive(this GameObject go, LayerMask mask)
        {
            int index = mask.ToIndex();
            go.layer = index;
            foreach (Transform child in go.transform)
            {
                child.gameObject.SetLayerRecursive(mask);
            }
        }
        
        /// <summary>
        /// Returns the string name of the first active layer in the mask.
        /// </summary>
        public static string ToLayerMaskName(this LayerMask mask)
        {
            // Use the existing ToIndex extension to get the layer ID
            int index = mask.ToIndex();
    
            // Convert ID to the name defined in Tags & Layers
            return LayerMask.LayerToName(index);
        }
        
        /// <summary>
        /// Checks if the mask contains ALL layers from the target mask.
        /// (Bitwise: mask & target == target)
        /// </summary>
        public static bool ContainsAll(this LayerMask mask, LayerMask target)
        {
            return (mask.value & target.value) == target.value;
        }

        /// <summary>
        /// Checks if the mask contains AT LEAST ONE layer from the target mask.
        /// (Bitwise: mask & target != 0)
        /// </summary>
        public static bool ContainsAny(this LayerMask mask, LayerMask target)
        {
            return (mask.value & target.value) != 0;
        }

        /// <summary>
        /// Checks if the LayerMask contains a specific layer index.
        /// </summary>
        public static bool ContainsLayer(this LayerMask mask, int layerIndex)
        {
            // Compare mask value with bit-shifted index
            return (mask.value & (1 << layerIndex)) != 0;
        }
        
        /// <summary>
        /// Compares two masks bit by bit. They must be identical.
        /// </summary>
        public static bool IsSame(this LayerMask mask, LayerMask target)
        {
            return mask.value == target.value;
        }
        
        /// <summary>
        /// Checks if the primary layer name of the mask matches the given string.
        /// </summary>
        public static bool IsLayerName(this LayerMask mask, string layerName)
        {
            return LayerMask.LayerToName(mask.ToIndex()) == layerName;
        }
        
        /// <summary>
        /// Returns a string representation of the mask in binary format (e.g., "0000...0101").
        /// Useful for debugging bitwise issues.
        /// </summary>
        public static string ToBinaryString(this LayerMask mask)
        {
            return System.Convert.ToString(mask.value, 2).PadLeft(32, '0');
        }
        
          
        /// <summary>
        /// Checks if the LayerMask is set to "Nothing" (value is 0).
        /// </summary>
        public static bool IsNothing(this LayerMask mask)
        {
            return mask.value == 0;
        }
        
        /// <summary>
        /// Checks if the mask is truly empty or only contains unnamed layers.
        /// </summary>
        public static bool IsTrulyNothing(this LayerMask mask)
        {
            // Standard check for bitwise zero
            if (mask.value == 0) return true;

            // Optional: Check if the bits that are set actually point to named layers
            for (int i = 0; i < 32; i++)
            {
                if ((mask.value & (1 << i)) != 0)
                {
                    // If at least one set bit has a name, it's not "Nothing"
                    if (!string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        
        /// Returns a cleaned mask containing only bits that correspond to actually named layers.
        /// Useful for fixing masks that show "Nothing" in inspector despite having bits set.
        /// </summary>
        public static LayerMask CleanUnnamedLayers(this LayerMask mask)
        {
            int cleanedValue = 0;
            for (int i = 0; i < 32; i++)
            {
                int bit = 1 << i;
                if ((mask.value & bit) != 0)
                {
                    // Only keep the bit if the layer has a name defined in Unity
                    if (!string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                    {
                        cleanedValue |= bit;
                    }
                }
            }
            return cleanedValue;
        }

        /// <summary>
        /// Checks if the LayerMask is set to "Everything" (all bits are 1, value is -1).
        /// </summary>
        public static bool IsEverything(this LayerMask mask)
        {
            // In Unity, "Everything" is represented by all bits set to 1, which equals -1 in decimal.
            return mask.value == -1;
        }
    }
}