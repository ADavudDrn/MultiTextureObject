using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class GenerateMultiTextureObject : MonoBehaviour
    {
        //-------Public Variables-------//


        //------Serialized Fields-------//
        [SerializeField] private Vector3 ObjectDimensions;

        [SerializeField] private int ObjectCountPerRow;
        [SerializeField] private int ObjectCountPerColumn;
        [SerializeField] private TextureObject TextureObjectPrefab;

        //------Private Variables-------//


        #region UNITY_METHODS

        private void Start()
        {
            GenerateRandomMultiTextureObject();
        }

        #endregion

        #region PUBLIC_METHODS

        public void GenerateRandomMultiTextureObject()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = Vector3.zero;
            cube.transform.localScale = ObjectDimensions;
            var textureParent = new GameObject("TextureParent");
            textureParent.transform.parent = cube.transform;
            textureParent.transform.localPosition = Vector3.zero;
            
            var texturesDimensionsX = ObjectDimensions.x / ObjectCountPerRow; 
            var texturesDimensionsZ = ObjectDimensions.z / ObjectCountPerColumn;
            var texturesDimensionsY = ObjectDimensions.y;
            
            var startPos = new Vector3(cube.transform.position.x - ObjectDimensions.x / 2f + texturesDimensionsX / 2f,
                cube.transform.position.y + .01f,
                cube.transform.position.z + ObjectDimensions.z / 2f - texturesDimensionsZ / 2f);

            for (int i = 0; i < ObjectCountPerColumn; i++)
            {
                for (int j = 0; j < ObjectCountPerRow; j++)
                {
                    var pos = startPos + new Vector3(j* texturesDimensionsX, 0, -i* texturesDimensionsZ);
                    var textureObject = Instantiate(TextureObjectPrefab, pos, Quaternion.identity, textureParent.transform);
                    textureObject.transform.localScale = new Vector3(texturesDimensionsX, texturesDimensionsY, texturesDimensionsZ);
                    textureObject.Stiffness = Random.Range(0f, 1f);
                    textureObject.Temperature = Random.Range(0f, 1f);
                    textureObject.Texture = Random.Range(0f, 1f);
                }
            }

        }

        #endregion

        #region PRIVATE_METHODS


        #endregion
    }

}