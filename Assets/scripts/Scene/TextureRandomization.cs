using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class TextureRandomization : MonoBehaviour
{
    //public var materialList : MaterialList[];
    [Serializable]
    public struct MaterialList{
        public Material[] materials;
    }
    [Serializable]
    public struct TextureList{
        public Texture[] textures;
    }
    public MaterialList[] materialList;
    public TextureList[] textureList;
    
    public void RandomizeMaterial(GameObject obj, int typeMat){
        if (typeMat < 0 || typeMat >= materialList.Length){
            typeMat = UnityEngine.Random.Range(0, materialList.Length);
        }
        // get material of the obj 
        Material mat = materialList[typeMat]
            .materials[UnityEngine.Random.Range(0, materialList[typeMat].materials.Length)];
        if (typeMat == 0){
            RandomizeGenericMaterial(mat);
        }
        obj.GetComponent<Renderer>().material = mat;
    }
    /// <summary>
    /// Randomizes the generic material by changing its texture, color, tiling and offset, and metallic and smoothness.
    /// </summary>
    /// <param name="mat">The material to be randomized.</param>
    public void RandomizeGenericMaterial(Material mat){
        // Randomize texture (Base Map)
        mat.mainTexture = 
            textureList[0]
                .textures[UnityEngine.Random.Range(0, textureList[0].textures.Length)];

        // Randomize Base Map Color
        mat.color = Color.HSVToRGB(
            UnityEngine.Random.Range(0f, 1f), 
            UnityEngine.Random.Range(0f, 0.5f),
            1f 
            //UnityEngine.Random.Range(0f, 1f)
        );
        // Randomize Tiling and Offset
        mat.mainTextureScale = new Vector2(
            UnityEngine.Random.Range(0f, 2f), 
            UnityEngine.Random.Range(0f, 2f));
        mat.mainTextureOffset = new Vector2(
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(0f, 1f)
        );
        // Randomize Metallic and Smoothness
    }
}
