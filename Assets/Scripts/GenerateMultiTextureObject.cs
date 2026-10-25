using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using WeArt.Core;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using Texture = WeArt.Core.Texture;

namespace DefaultNamespace
{
    [Serializable]
    public struct LetterTextureMapping
    {
        public string Letter;
        public TextureType Texture;
    }

    public class GenerateMultiTextureObject : MonoBehaviour
    {
        [SerializeField] private Vector3 ObjectDimensions = new Vector3(1, 1, 1);
        [SerializeField] private int ObjectCountPerRow = 10;
        [SerializeField] private int ObjectCountPerColumn = 10;
        [SerializeField] private TextureObject TextureObjectPrefab;
        
        [Header("UI & File Loading")]
        [Tooltip("Assign your TextMeshPro Dropdown UI here")]
        [SerializeField] private TMP_Dropdown CsvDropdown;
        [SerializeField] private char CsvSeparator = ',';

        [Header("Texture Mappings")]
        [SerializeField] private List<LetterTextureMapping> TextureMappings = new List<LetterTextureMapping>();

        private Dictionary<string, TextureType> _textureDict = new Dictionary<string, TextureType>();
        private Dictionary<Vector2Int, CellData> _gridDataMap = new Dictionary<Vector2Int, CellData>();

        // Tracks the currently generated object so we can delete it later
        private GameObject _generatedRootObject;
        private string[] _availableCsvPaths;

        private float _minX, _maxX, _minZ, _maxZ, _minY, _maxY;

        private class CellData
        {
            public float SumStiffnessY = 0f;
            public float SumTemperature = 0f;
            public int DataPointsCount = 0;
            public Dictionary<string, int> LetterFrequencies = new Dictionary<string, int>();
        }

        private class RawDataPoint
        {
            public float X, Y, Z;
            public string SourceName;
            public string SourceType;
        }

        #region UNITY_METHODS

        private void Start()
        {
            foreach (var mapping in TextureMappings)
            {
                if (!_textureDict.ContainsKey(mapping.Letter))
                    _textureDict.Add(mapping.Letter, mapping.Texture);
            }

            PopulateDropdown();
        }

        #endregion

        #region PUBLIC_METHODS

        // Link this method to your UI Button's OnClick event
        public void OnGenerateButtonClicked()
        {
            if (_availableCsvPaths == null || _availableCsvPaths.Length == 0)
            {
                Debug.LogWarning("No CSV files found to generate from.");
                return;
            }

            string selectedFilePath = _availableCsvPaths[CsvDropdown.value];
            if (!File.Exists(selectedFilePath))
            {
                Debug.LogError($"File not found at: {selectedFilePath}");
                return;
            }

            string csvContent = File.ReadAllText(selectedFilePath);
            
            ParseAndMapCSVData(csvContent);
            GenerateSimulatedMultiTextureObject();
        }

        public void GenerateSimulatedMultiTextureObject()
        {
            // Delete the previously generated object if it exists
            if (_generatedRootObject != null)
            {
                Destroy(_generatedRootObject);
            }

            _generatedRootObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _generatedRootObject.transform.position = Vector3.zero;
            _generatedRootObject.transform.localScale = ObjectDimensions;
            
            var textureParent = new GameObject("TextureParent");
            textureParent.transform.parent = _generatedRootObject.transform;
            textureParent.transform.localPosition = Vector3.zero;
            
            var texturesDimensionsX = ObjectDimensions.x / ObjectCountPerRow; 
            var texturesDimensionsZ = ObjectDimensions.z / ObjectCountPerColumn;
            var texturesDimensionsY = ObjectDimensions.y;
            
            var startPos = new Vector3(_generatedRootObject.transform.position.x - ObjectDimensions.x / 2f + texturesDimensionsX / 2f,
                _generatedRootObject.transform.position.y + .01f,
                _generatedRootObject.transform.position.z + ObjectDimensions.z / 2f - texturesDimensionsZ / 2f);

            for (int row = 0; row < ObjectCountPerColumn; row++)
            {
                for (int col = 0; col < ObjectCountPerRow; col++)
                {
                    var pos = startPos + new Vector3(col * texturesDimensionsX, 0, -row * texturesDimensionsZ);
                    var textureObject = Instantiate(TextureObjectPrefab, pos, Quaternion.identity, textureParent.transform);
                    textureObject.transform.localScale = new Vector3(texturesDimensionsX, texturesDimensionsY, texturesDimensionsZ);

                    Vector2Int gridCoord = new Vector2Int(row, col);

                    if (_gridDataMap.TryGetValue(gridCoord, out CellData cell) && cell.DataPointsCount > 0)
                    {
                        float avgY = cell.SumStiffnessY / cell.DataPointsCount;
                        float avgTemperature = cell.SumTemperature / cell.DataPointsCount; 
                        float normalizedStiffness = Mathf.InverseLerp(_minY, _maxY, avgY);
                        
                        string dominantType = cell.LetterFrequencies.OrderByDescending(kvp => kvp.Value).First().Key;
                        
                        TextureType selectedTexture = TextureType.Aluminium;
                        if (dominantType.Length == 1 && _textureDict.TryGetValue(dominantType, out TextureType mappedTexture))
                        {
                            selectedTexture = mappedTexture;
                        }

                        textureObject.SetTouchableObject(avgTemperature, normalizedStiffness, selectedTexture);
                    }
                    else
                    {
                        textureObject.SetTouchableObject(0f, 0f, TextureType.Aluminium); 
                    }
                }
            }
            Debug.Log("Object Generation Complete.");
        }

        #endregion

        #region PRIVATE_METHODS

        private void PopulateDropdown()
        {
            if (CsvDropdown == null)
            {
                Debug.LogError("CSV Dropdown is not assigned in the Inspector.");
                return;
            }

            string dataFolderPath = Path.Combine(Application.streamingAssetsPath, "Data");
            
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
                Debug.LogWarning($"Created Data folder at {dataFolderPath}. Please place CSVs inside it.");
                return;
            }

            _availableCsvPaths = Directory.GetFiles(dataFolderPath, "*.csv");
            
            CsvDropdown.ClearOptions();
            List<string> fileNames = new List<string>();

            foreach (string path in _availableCsvPaths)
            {
                fileNames.Add(Path.GetFileName(path));
            }

            CsvDropdown.AddOptions(fileNames);
        }
        
        private void ParseAndMapCSVData(string csvText)
        {
            // Reset grid and min/max trackers before parsing a new file
            _gridDataMap.Clear();
            _minX = float.MaxValue; _maxX = float.MinValue;
            _minZ = float.MaxValue; _maxZ = float.MinValue;
            _minY = float.MaxValue; _maxY = float.MinValue;

            string[] lines = csvText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            Regex csvParser = new Regex(CsvSeparator + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            string[] headers = csvParser.Split(lines[0]);
            int xCol = -1, yCol = -1, zCol = -1, nameCol = -1, typeCol = -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].ToLower().Trim(' ', '"');
                if (h.Contains("x_unity")) xCol = i;
                if (h.Contains("y_unity")) yCol = i;
                if (h.Contains("z_unity")) zCol = i;
                if (h.Contains("source_name")) nameCol = i;
                if (h.Contains("source_type")) typeCol = i;
            }

            if (xCol == -1 || yCol == -1 || zCol == -1 || nameCol == -1 || typeCol == -1) return;

            List<RawDataPoint> rawDataList = new List<RawDataPoint>();

            for (int i = 1; i < lines.Length; i++)
            {
                string[] columns = csvParser.Split(lines[i]);
                if (columns.Length <= Mathf.Max(xCol, yCol, zCol, nameCol, typeCol)) continue;

                string xStr = columns[xCol].Trim(' ', '"').Replace(",", ".");
                string yStr = columns[yCol].Trim(' ', '"').Replace(",", ".");
                string zStr = columns[zCol].Trim(' ', '"').Replace(",", ".");
                
                if (float.TryParse(xStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float xVal) &&
                    float.TryParse(yStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float yVal) &&
                    float.TryParse(zStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float zVal))
                {
                    if (xVal < _minX) _minX = xVal;
                    if (xVal > _maxX) _maxX = xVal;
                    if (zVal < _minZ) _minZ = zVal;
                    if (zVal > _maxZ) _maxZ = zVal;
                    if (yVal < _minY) _minY = yVal;
                    if (yVal > _maxY) _maxY = yVal;

                    rawDataList.Add(new RawDataPoint
                    {
                        X = xVal, Y = yVal, Z = zVal,
                        SourceName = columns[nameCol].Trim(' ', '"'),
                        SourceType = columns[typeCol].Trim(' ', '"')
                    });
                }
            }

            foreach (var pt in rawDataList)
            {
                int col = Mathf.Clamp(Mathf.FloorToInt(((pt.X - _minX) / (_maxX - _minX)) * ObjectCountPerRow), 0, ObjectCountPerRow - 1);
                int row = Mathf.Clamp(Mathf.FloorToInt(((pt.Z - _minZ) / (_maxZ - _minZ)) * ObjectCountPerColumn), 0, ObjectCountPerColumn - 1);
                
                Vector2Int gridCoord = new Vector2Int(row, col);

                if (!_gridDataMap.ContainsKey(gridCoord))
                {
                    _gridDataMap[gridCoord] = new CellData();
                }

                CellData cell = _gridDataMap[gridCoord];
                cell.SumStiffnessY += pt.Y;
                
                if (pt.SourceName.Contains("3_mm"))
                {
                    cell.SumTemperature += 1f;
                }
                
                if (cell.LetterFrequencies.ContainsKey(pt.SourceType))
                    cell.LetterFrequencies[pt.SourceType]++;
                else
                    cell.LetterFrequencies[pt.SourceType] = 1;

                cell.DataPointsCount++;
            }
        }

        #endregion
    }
}