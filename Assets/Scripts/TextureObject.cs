using UnityEngine;
using WeArt.Components;
using WeArt.Core;
using Texture = WeArt.Core.Texture;

namespace DefaultNamespace
{
    public class TextureObject : MonoBehaviour
    {
        //-------Public Variables-------//


        //------Serialized Fields-------//
        [SerializeField] private WeArtTouchableObject _touchableObject;

        //------Private Variables-------//
        


        #region UNITY_METHODS
        

        #endregion

        #region PUBLIC_METHODS

        public void SetTouchableObject(float temperature, float stiffness, TextureType textureType)
        {
            _touchableObject.SetTemperature(temperature);
            _touchableObject.SetHapticForce(stiffness);
            _touchableObject.SetTextureType(textureType);
        }

        #endregion

        #region PRIVATE_METHODS


        #endregion
    }

}