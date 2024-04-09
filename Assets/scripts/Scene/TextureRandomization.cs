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
    public MaterialList[] materialList;
    public void RandomizeMaterial(GameObject obj, int typeMat){
        // get material of the obj 
        Material mat = materialList[typeMat]
            .materials[UnityEngine.Random.Range(0, materialList[typeMat].materials.Length)];
        obj.GetComponent<Renderer>().material = mat;
    }
}
