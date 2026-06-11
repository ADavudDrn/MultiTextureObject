using System;
using System.Collections.Generic;
using UnityEngine;
using WeArt.Core;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;
using Texture = WeArt.Core.Texture;

namespace DefaultNamespace
{
    public class GenerateMultiTextureObject : MonoBehaviour
    {
        [SerializeField] private Vector3 ObjectDimensions;
        [SerializeField] private int ObjectCountPerRow;
        [SerializeField] private int ObjectCountPerColumn;
        [SerializeField] private TextureObject TextureObjectPrefab;
        
        [Header("Data Simulation")]
        [Tooltip("Assign your exported MATLAB CSV file here")]
        [SerializeField] private TextAsset SimulationDataCSV; 
        
        [Tooltip("If '0 points parsed', change this to ';' (common in European Excel settings)")]
        [SerializeField] private char CsvSeparator = ',';

        // Dictionary to store data mapped by (row, column)
        private Dictionary<Vector2Int, CellData> _tissueDataMap = new Dictionary<Vector2Int, CellData>();
        
        // Track the max dimensions of the CSV to calculate downsampling ratios
        private int _maxCsvRow = 0;
        private int _maxCsvCol = 0;

        // Track Min/Max values for Lerping (Normalization)
        private float _minY = float.MaxValue;
        private float _maxY = float.MinValue;
        private float _minHeat = float.MaxValue;
        private float _maxHeat = float.MinValue;

        // Internal class to hold the parsed MATLAB data
        private class CellData
        {
            public float Stiffness;
            public float Temperature;
            public TextureType TextureType = TextureType.Aluminium;
        }

        #region UNITY_METHODS

        private void Start()
        {
            ParseCSVData();
            GenerateSimulatedMultiTextureObject();
        }

        #endregion

        #region PUBLIC_METHODS

        public void GenerateSimulatedMultiTextureObject()
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

            float rowRatio = _maxCsvRow > 0 ? (float)_maxCsvRow / ObjectCountPerColumn : 1f;
            float colRatio = _maxCsvCol > 0 ? (float)_maxCsvCol / ObjectCountPerRow : 1f;

            for (int row = 0; row < ObjectCountPerColumn; row++)
            {
                for (int col = 0; col < ObjectCountPerRow; col++)
                {
                    var pos = startPos + new Vector3(col * texturesDimensionsX, 0, -row * texturesDimensionsZ);
                    var textureObject = Instantiate(TextureObjectPrefab, pos, Quaternion.identity, textureParent.transform);
                    textureObject.transform.localScale = new Vector3(texturesDimensionsX, texturesDimensionsY, texturesDimensionsZ);

                    int startCsvRow = Mathf.FloorToInt(row * rowRatio) + 1; 
                    int endCsvRow = Mathf.FloorToInt((row + 1) * rowRatio);
                    
                    int startCsvCol = Mathf.FloorToInt(col * colRatio) + 1;
                    int endCsvCol = Mathf.FloorToInt((col + 1) * colRatio);

                    if (endCsvRow < startCsvRow) endCsvRow = startCsvRow;
                    if (endCsvCol < startCsvCol) endCsvCol = startCsvCol;

                    float sumTemperature = 0f;
                    float sumStiffness = 0f;
                    int dataPointsFound = 0;
                    TextureType fallbackTexture = TextureType.Aluminium;

                    for (int csvRow = startCsvRow; csvRow <= endCsvRow; csvRow++)
                    {
                        for (int csvCol = startCsvCol; csvCol <= endCsvCol; csvCol++)
                        {
                            Vector2Int csvCoord = new Vector2Int(csvRow, csvCol);
                            if (_tissueDataMap.TryGetValue(csvCoord, out CellData data))
                            {
                                sumTemperature += data.Temperature;
                                sumStiffness += data.Stiffness;
                                fallbackTexture = data.TextureType; 
                                dataPointsFound++;
                            }
                        }
                    }

                    if (dataPointsFound > 0)
                    {
                        float avgTemperature = sumTemperature / dataPointsFound;
                        float avgStiffness = sumStiffness / dataPointsFound;
                        
                        textureObject.SetTouchableObject(avgTemperature, avgStiffness, fallbackTexture);
                    }
                    else
                    {
                        textureObject.SetTouchableObject(0f, 0f, TextureType.Aluminium); 
                    }
                }
            }
        }

        #endregion

        #region PRIVATE_METHODS
        
        private void ParseCSVData()
        {
            if (SimulationDataCSV == null)
            {
                Debug.LogError("SimulationDataCSV is not assigned! Please assign the text asset.");
                return;
            }

            string[] lines = SimulationDataCSV.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1) return;

            Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            string[] headers = csvParser.Split(lines[0]);
            int rowIdx = -1, colIdx = -1, heatCol = -1, yCol = -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].ToLower().Trim(' ', '"');
                if (h.Contains("grid_row") || h == "row") rowIdx = i;
                if (h.Contains("grid_col") || h == "column") colIdx = i;
                if (h.Contains("heat_color_scalar")) heatCol = i;
                if (h.Contains("y_unity")) yCol = i;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string[] columns = csvParser.Split(lines[i]);

                for (int c = 0; c < columns.Length; c++)
                {
                    columns[c] = columns[c].Trim(' ', '"');
                }

                if (rowIdx != -1 && colIdx != -1 && columns.Length > Mathf.Max(rowIdx, colIdx)) 
                {
                    if (int.TryParse(columns[rowIdx], out int row) && int.TryParse(columns[colIdx], out int col))
                    {
                        if (row > _maxCsvRow) _maxCsvRow = row;
                        if (col > _maxCsvCol) _maxCsvCol = col;

                        CellData cellData = new CellData();

                        if (heatCol != -1 && columns.Length > heatCol)
                        {
                            string heatStr = columns[heatCol].Replace(",", "."); 
                            if (float.TryParse(heatStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsedHeat))
                            {
                                cellData.Temperature = parsedHeat;
                                if (parsedHeat < _minHeat) _minHeat = parsedHeat;
                                if (parsedHeat > _maxHeat) _maxHeat = parsedHeat;
                            }
                        }

                        if (yCol != -1 && columns.Length > yCol)
                        {
                            string yStr = columns[yCol].Replace(",", ".");
                            if (float.TryParse(yStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsedY))
                            {
                                cellData.Stiffness = parsedY;
                                if (parsedY < _minY) _minY = parsedY;
                                if (parsedY > _maxY) _maxY = parsedY;
                            }
                        }

                        _tissueDataMap[new Vector2Int(row, col)] = cellData;
                    }
                }
            }
            
            foreach (var kvp in _tissueDataMap)
            {
                CellData data = kvp.Value;
                
                data.Stiffness = Mathf.InverseLerp(_minY, _maxY, data.Stiffness);
                data.Temperature = Mathf.InverseLerp(_minHeat, _maxHeat, data.Temperature);
            }
            
            Debug.Log($"Parsed {_tissueDataMap.Count} points. " +
                      $"Y (Stiffness) Min/Max: {_minY}/{_maxY}. " +
                      $"Heat Min/Max: {_minHeat}/{_maxHeat}");
        }

        #endregion
    }
}