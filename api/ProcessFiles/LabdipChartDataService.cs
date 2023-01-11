using BrandixAutomation.Labdip.API.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BrandixAutomation.Labdip.API.ProcessFiles
{
    public class LabdipChartDataService
    {
        private LabdipChartModel _labdipChartModel;
        private List<LabdipChartModel> _labdipChartModelList;
        private Sheet _sheet;
        public LabdipChartDataService()
        {
            _labdipChartModel = new LabdipChartModel();
            _labdipChartModelList = new List<LabdipChartModel>();
            _sheet = new Sheet("MasterTemp");
        }
        public List<LabdipChartModel> GetLabdipChartData(IFormFile file) //byte[] excelSheetByteArray
        {
            _sheet = ReadDataIntoSheet(file);

            //1 Set Divion
            SetDivision();

            //2 Season and Category
            Season_CategoryNew();

            //3 Styele No and DEscription
            StyleNoIndividual_GMTDescription();


            //var result = GetSectionDetails(_sheet);

            //4 GMTColor RMColor
            _labdipChartModelList = PrepareLabdipChart(ColorwayPivotSheet(GMT_Color_RMColor_ModelSearch(_sheet),GetSectionDetails(_sheet)));

            PrepareNRFandColorCode();

            SetColorDyeingTechnic();

            return _labdipChartModelList;
        }

        private Sheet ReadDataIntoSheet(IFormFile file) //byte[] excelSheetByteArray
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var sheet = new Sheet("TempMaster");

            //var tempByteArray = FileToByteArray(@"C:\WorkSpaces\TechPacks.xlsx");           
           
            //using (var stream = new MemoryStream(tempByteArray))
            //{
                using (var reader = ExcelReaderFactory.CreateReader(file.OpenReadStream()))
                {
                    //columnCount = reader.FieldCount;
                    //rowCount = reader.RowCount;
                    do
                    {
                        int r = 0;
                        while (reader.Read())
                        {
                            var row = new Row();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row.Cells.Add(PrepareCell(i, reader.GetValue(i)));
                            }
                            r++;
                            sheet.Rows.Add(row);
                        }
                        reader.Close(); //only one Sheet reading..
                    } while (reader.NextResult());
                }
            //}
            return sheet;
        }

        private Cell PrepareCell(int index, object cellValue)
        {
            return new Cell()
            {
                ColIndex = index,
                ColValue = cellValue != null ? cellValue.ToString() : "Null"
            };
        }

        private void SetDivision()
        {
            bool found = false;

            _sheet.Rows.ForEach(row =>
            {
                row.Cells.ForEach(cell =>
                {
                    if ((found) && (cell.ColValue != null) && (!cell.ColValue.Equals("Null")))
                    {
                        _labdipChartModel.Division = cell.ColValue;
                        found = false;
                    }

                    if ((cell.ColValue != null) && (cell.ColValue.Equals("Sub - Brand")))
                        found = true;
                });
            });
        }

        private void Season_Category()
        {
            _sheet.Rows.ForEach(row =>
            {
                row.Cells.ForEach((cell) =>
                {
                    if ((cell.ColValue != null) && cell.ColValue.Contains("Season Name"))
                    {
                        //int i = 0;
                        string result_season = "";
                        string result_category = "";

                        var temp = cell.ColValue.Split(' ');

                        bool found = false;

                        for (int i = 0; i < temp.Length; i++)
                        {

                            if (temp[i].Contains("Season") & temp[(i + 1)].Contains("Name"))
                            {
                                result_season = temp[i + 2] + temp[i + 3];
                                result_category = temp[i + 4] + temp[i+5];
                            }
                        }

                        //foreach (string word in cell.ColValue.Split(' '))
                        //{
                        //    i++;

                        //    if (word.Contains("Season"))
                        //    {
                        //        var foundIndex_season = i;
                        //    }

                        //    if (i > 2 && i < 5)
                        //        result_season = result_season + ' ' + word;

                        //    if (i > 4 && i < 7)
                        //        result_category = result_category + ' ' + word;
                        //}
                        _labdipChartModel.Season = result_season;
                        _labdipChartModel.Category = result_category;
                    }
                });
            });
        }

        private void Season_CategoryNew()
        {
            string[] seasonCategoryCellData = null;
            bool found = false;
            foreach (var row in _sheet.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    if ((cell.ColValue != null) && cell.ColValue.Contains("Season Name"))
                    {
                        seasonCategoryCellData = cell.ColValue.Split(' ');
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            if(seasonCategoryCellData.Length > 0 && seasonCategoryCellData != null)
            {
                for (int i = 0; i < seasonCategoryCellData.Length; i++)
                {
                    if (seasonCategoryCellData[i].Contains("Season") & ((i+1)<=(seasonCategoryCellData.Length-1))) //stop run ot of array index
                    {
                        if(seasonCategoryCellData[(i + 1)].Contains("Name"))
                        {
                            _labdipChartModel.Season = seasonCategoryCellData[i + 2] + " " + seasonCategoryCellData[i + 3];
                            _labdipChartModel.Category = seasonCategoryCellData[i + 4] +" " + seasonCategoryCellData[i + 5];
                            break;
                        }
                    }
                }               
            }
        }

        private void StyleNoIndividual_GMTDescription()
        {
            bool found = false;

            _sheet.Rows.ForEach((row) =>
            {
                row.Cells.ForEach(cell =>
                {
                    if ((found) && (cell.ColValue != null) && (!cell.ColValue.Equals("Null")))
                    {
                        _labdipChartModel.StyleNoIndividual = cell.ColValue.Substring(0, cell.ColValue.IndexOf('/'));
                        _labdipChartModel.GMTDescription = cell.ColValue.Substring(cell.ColValue.IndexOf("/") + 1); //each.Substring(each.IndexOf("/")+1, each.IndexOf('/')-2);
                        found = false;
                    }

                    if ((cell.ColValue != null) && (cell.ColValue.Equals("Product Code / Description:")))
                    {
                        found = true;
                    }
                });
            });
        }

        public List<Sheet> GMT_Color_RMColor_ModelSearch(Sheet tempSheet)
        {
            bool found = false;
            int colorwayHeaderFoundColIndex = 0;
            int colorwayHeaderFoundRowIndex = 0;
            int colorwayHeaderFoundColFrequency = 0;
            Sheet colorWaySheet = new Sheet("Temp");
            List<Sheet> colorWayExtractedList = new List<Sheet>();

            for (int r = 0; r < tempSheet.Rows.Count; r++)
            {
                Row colorwayRow = new Row();
                foreach (Cell cell in tempSheet.Rows[r].Cells)
                {
                    if (found)
                    {
                        if ((cell.ColIndex >= colorwayHeaderFoundColIndex) && (cell.ColIndex <= (colorwayHeaderFoundColIndex + 30)) && (r > colorwayHeaderFoundRowIndex))
                        {
                            if ((colorwayRow.Cells.Count < 10) && (ValidateColumnData(tempSheet.Rows[r], cell, colorWaySheet.Rows.Count > 0 ? colorWaySheet.Rows[0] : null, tempSheet.Rows[colorwayHeaderFoundRowIndex + 1])))
                            {
                                if ((cell.ColValue != null) && (!cell.ColValue.Equals("Null")))
                                {
                                    var cellHeader = new Cell() { ColIndex = cell.ColIndex, ColValue = cell.ColValue.Replace("\n", "").Replace("\r", "") };
                                    colorwayRow.Cells.Add(cellHeader);
                                }
                            }
                        }
                    }

                    if ((cell.ColValue != null) && cell.ColValue.ToLower().Equals("colorway") || (r == (tempSheet.Rows.Count - 1)))
                    {
                        found = true;
                        if (colorWaySheet.Rows.Count > 0)
                        {
                            Sheet newSheet = new Sheet("Colorway");
                            newSheet.Rows.AddRange(colorWaySheet.Rows);
                            colorWayExtractedList.Add(newSheet);
                            colorWaySheet.Rows.Clear();
                        }
                        else
                        {
                            if (colorwayHeaderFoundColFrequency > 0)
                            {
                                Sheet newSheet = new Sheet("Colorway");
                                colorWayExtractedList.Add(newSheet);
                                colorWaySheet.Rows.Clear();
                            }
                            
                        }
                        colorwayHeaderFoundColIndex = cell.ColIndex;
                        colorwayHeaderFoundRowIndex = r;
                        colorwayHeaderFoundColFrequency ++;
                        break;
                    }
                };
                if (colorwayRow.Cells.Count > 0)
                    colorWaySheet.Rows.Add(colorwayRow);
            };
            return colorWayExtractedList;
        }

        private List<Sheet> GetSectionDetails(Sheet tempSheet)
        {
            bool found = false;
            int sectioneaderFoundColIndex = 0;
            int sectionHeaderFoundRowIndex = 0;
            Sheet sectionSheet = new Sheet("Temp");
            List<Sheet> sectionExtractedList = new List<Sheet>();
            for (int r = 0; r < tempSheet.Rows.Count; r++)
            {
                Row sectionRow = new Row();
                foreach (Cell cell in tempSheet.Rows[r].Cells)
                {
                    if (found)
                    {
                        if ((cell.ColIndex >= sectioneaderFoundColIndex) && (cell.ColIndex <= (sectioneaderFoundColIndex + 30)) && (r > sectionHeaderFoundRowIndex))
                        {
                            if ((sectionRow.Cells.Count < 7) && (ValidateColumnData(tempSheet.Rows[r], cell, sectionSheet.Rows.Count > 0 ? sectionSheet.Rows[0] : null, tempSheet.Rows[sectionHeaderFoundRowIndex + 1])))
                            {
                                if (sectionSheet.Rows.Count > 0) //Column Data
                                {
                                    if ((cell.ColValue != null))
                                    {
                                        var cellHeader = new Cell() { ColIndex = cell.ColIndex, ColValue = cell.ColValue.Replace("\n", "").Replace("\r", "") };
                                        sectionRow.Cells.Add(cellHeader);
                                    }
                                }
                                else //Headers reading
                                {
                                    if ((cell.ColValue != null) && (!cell.ColValue.Equals("Null")))
                                    {
                                        var cellHeader = new Cell() { ColIndex = cell.ColIndex, ColValue = cell.ColValue.Replace("\n", "").Replace("\r", "") };
                                        sectionRow.Cells.Add(cellHeader);
                                    }
                                }
                            }
                        }
                    }


                    if ((cell.ColValue != null) && cell.ColValue.ToLower().Contains("section:") || (r == (tempSheet.Rows.Count - 1)))
                    {
                        found = true;
                        //First sheet detection
                        if (sectionSheet.Rows.Count == 0) sectionSheet.SheetName = cell.ColValue.Substring(cell.ColValue.IndexOf(":") + 1);

                        if (sectionSheet.Rows.Count > 0)
                        {
                            Sheet newSheet = new Sheet(sectionSheet.SheetName);
                            newSheet.Rows.AddRange(sectionSheet.Rows);
                            sectionExtractedList.Add(newSheet);
                            sectionSheet.Rows.Clear();
                            sectionSheet.SheetName = cell.ColValue.Substring(cell.ColValue.IndexOf(":") + 1);
                        }
                        sectioneaderFoundColIndex = cell.ColIndex;
                        sectionHeaderFoundRowIndex = r;
                        break;
                    }
                }
                if (sectionRow.Cells.Count > 0)
                    sectionSheet.Rows.Add(sectionRow);
            }
            return sectionExtractedList;
        }

        private void PrepareNRFandColorCode()
        {
            _labdipChartModelList.ForEach(each =>
            {
                if (each.GMTColor != null)
                {
                    var indexLocation = each.GMTColor.LastIndexOf("(");
                    var endTale = each.GMTColor.Substring(indexLocation - 5);
                    var nrf = endTale.Substring(0, 5);
                    each.NRF = nrf;
                }else
                    each.NRF = null;
               

                //setting color codes
                // same thing can get from ection data area (part Type Col)
                if(each.RMColor != null)
                {
                    string colorCode = null;
                    if (each.BOMSelection.ToLower().Equals("fabric"))
                    {
                        var wordArray = each.RMColor.Split(' ');
                        if(wordArray.Length > 1)
                        {
                            foreach(var word in wordArray)
                            {
                                colorCode = (word.Length == 4) ? Regex.IsMatch(word, @"^[a-zA-Z0-9]+$") ? word :null : null; //Regex reemove non leter and number fields, 
                            }
                        }
                    }
                    each.ColorCode = colorCode;
                }else
                    each.ColorCode= null;
                
            });
        }

        private void SetColorDyeingTechnic()
        {
            _labdipChartModelList.ForEach((each) =>
            {
                if((each.PalcementName != null) && (each.BOMSelection != null) && (each.ItemName != null))
                {
                    if (each.BOMSelection.Contains("Fabric"))
                    {
                        if (each.Index < _labdipChartModelList.Count)
                        {
                            if((_labdipChartModelList[each.Index].BOMSelection != null) && (_labdipChartModelList[each.Index].PalcementName != null))
                            {
                                if (_labdipChartModelList[each.Index].BOMSelection.ToLower().Contains("fabric") && _labdipChartModelList[each.Index].PalcementName.ToLower().Contains("dye"))
                                {
                                    each.ColorDyeingTechnic = _labdipChartModelList[each.Index].RMColor;
                                }
                            }                            
                        }
                    }
                }                
            });
        }

        private bool ValidateColumnData(Row row, Cell currentCell, Row firstRow=null, Row sectionHeaderRow=null)
        {
            bool result = false;
            row.Cells.ForEach(cell =>
            {
                //if ((cell.ColValue != null) && ((cell.ColValue.Equals("Quantity")) || cell.ColValue.Equals("0")) && (cell.ColIndex == 52))
                if ((cell.ColValue != null) && (cell.ColValue.ToLower().Contains("quantity") || (cell.ColValue.ToLower().Contains("price")) || (decimal.TryParse(cell.ColValue, out decimal cellval))))
                {
                    if (firstRow == null)
                    {
                        result = ValidateColorwayHeader(currentCell.ColValue) || ValidateSectionHeaderData(currentCell.ColValue) ? true : false;
                    }
                    else
                    {
                        result = currentCell.ColIndex < cell.ColIndex ? ColumAvailability(firstRow, currentCell,row, sectionHeaderRow) : false;
                    }
                }
            });
            return result;
        }

        private bool ValidateColorwayHeader(string cellContent)
        {
            string patternText = @"[(][0-9]{8}[)]$";
            Regex reg = new Regex(patternText);
            return reg.IsMatch(cellContent) ? true : false;
        }

        private bool ValidateSectionHeaderData(string cellContent)
        {
            List<string> columnHeaderNames = new List<string>(new string[] { "Part Type", "Part Name", "Material Id", "Material", "Over-ride", "Supplier Quality Number", "Supplier", "Use (Placement)" });

            return columnHeaderNames.Contains(cellContent) ? true : false;
        }

        private bool ColumAvailability(Row firstRow, Cell currentCell,Row currentRow,Row sectionHeaderRow)
        {
            if (firstRow == null) return true;
            bool result = false;

            //var firstRowEndIndex = sectionHeaderRow.Cells.FindIndex(ele => ele.ColValue.Equals("Quantity") || ele.ColValue.Equals("Price"));
            //if(firstRowEndIndex > 0)
            //{
            //    if((decimal.TryParse(currentRow.Cells[firstRowEndIndex].ColValue, out decimal cellval)))
            //        result = true;
            //}


            if (firstRow.Cells.FindIndex(ele=> ele.ColIndex == currentCell.ColIndex) != -1)
            {
                var firstRowEndIndex = sectionHeaderRow.Cells.FindIndex(ele => ele.ColValue.ToLower().Contains("quantity") || ele.ColValue.ToLower().Contains("price"));
                if (firstRowEndIndex > 0)
                {
                    if ((decimal.TryParse(currentRow.Cells[firstRowEndIndex].ColValue, out decimal cellval)))
                        result = true;
                }
                //result = true;
            }

            //firstRow.Cells.ForEach(cell =>
            //{
            //    if (cell.ColIndex == currentCell.ColIndex)
            //        result = true;
            //});
            return result;
        }

        private Sheet ColorwayPivotSheet(List<Sheet> colorwayExtractedData, List<Sheet> sectionExtractedData)
        {
            Sheet colorwayPivotSheet = new Sheet("ColorwayPivot");
            int sectionSheetNo = 0;
            
                colorwayExtractedData.ForEach(sheet =>
                {
                    if(sheet.Rows.Count > 0)
                    {
                        //column count
                        for (int c = 0; c < sheet.Rows[0].Cells.Count; c++)
                        {
                            //row by row
                            for (int r = 1; r < sheet.Rows.Count; r++)
                            {
                                Cell GMTColor = new Cell() { ColIndex = 0, ColValue = sheet.Rows[0].Cells[c].ColValue };
                                Cell RMColor = new Cell() { ColIndex = r, ColValue = sheet.Rows[r].Cells[c].ColValue };
                                Cell partName = new Cell() { ColIndex = 2, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Part Name", "Part Type" })) };
                                Cell materialId = new Cell() { ColIndex = 3, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Material Id" })) };
                                Cell material = new Cell() { ColIndex = 4, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Material" })) };
                                Cell supplierQNo = new Cell() { ColIndex = 5, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Supplier Quality Number" })) };
                                Cell supplier = new Cell() { ColIndex = 6, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Supplier" })) };
                                Cell usePalcemant = new Cell() { ColIndex = 7, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Use (Placement)" })) };
                                Cell section = new Cell() { ColIndex = 8, ColValue = NullReplacement(sectionExtractedData[sectionSheetNo].SheetName) };

                                Row row = new Row();
                                row.Cells.Add(GMTColor);
                                row.Cells.Add(RMColor);
                                row.Cells.Add(partName);
                                row.Cells.Add(materialId);
                                row.Cells.Add(material);
                                row.Cells.Add(supplierQNo);
                                row.Cells.Add(supplier);
                                row.Cells.Add(usePalcemant);
                                row.Cells.Add(section);
                                colorwayPivotSheet.Rows.Add(row);
                            }
                        }
                    }
                    else
                    {
                        //for(int c=0; c < sectionExtractedData[sectionSheetNo].Rows.Count; c++)
                        //{
                        if (sectionExtractedData[sectionSheetNo].Rows.Count > 1)
                        {
                            for (int r = 1; r < sectionExtractedData[sectionSheetNo].Rows.Count; r++)
                            {
                                Cell GMTColor = new Cell() { ColIndex = 0, ColValue = null };
                                Cell RMColor = new Cell() { ColIndex = 1, ColValue = null };
                                Cell partName = new Cell() { ColIndex = 2, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Part Name", "Part Type" })) };
                                Cell materialId = new Cell() { ColIndex = 3, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Material Id" })) };
                                Cell material = new Cell() { ColIndex = 4, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Material" })) };
                                Cell supplierQNo = new Cell() { ColIndex = 5, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Supplier Quality Number" })) };
                                Cell supplier = new Cell() { ColIndex = 6, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Supplier" })) };
                                Cell usePalcemant = new Cell() { ColIndex = 7, ColValue = NullReplacement(SetSectionData(sectionExtractedData[sectionSheetNo].Rows[0], sectionExtractedData[sectionSheetNo].Rows[r], new string[] { "Use (Placement)" })) };
                                Cell section = new Cell() { ColIndex = 8, ColValue = NullReplacement(sectionExtractedData[sectionSheetNo].SheetName) };

                                Row row = new Row();
                                row.Cells.Add(GMTColor);
                                row.Cells.Add(RMColor);
                                row.Cells.Add(partName);
                                row.Cells.Add(materialId);
                                row.Cells.Add(material);
                                row.Cells.Add(supplierQNo);
                                row.Cells.Add(supplier);
                                row.Cells.Add(usePalcemant);
                                row.Cells.Add(section);
                                colorwayPivotSheet.Rows.Add(row);
                            }
                        }
                    }                    
                    sectionSheetNo++;
                }); 
            
            return colorwayPivotSheet;
        }

        private List<LabdipChartModel> PrepareLabdipChart(Sheet colorwayPivotSheet)
        {
            List<LabdipChartModel> labdipChartModels = new List<LabdipChartModel>();
            int index = 1;
            colorwayPivotSheet.Rows.ForEach(row =>
            {
                LabdipChartModel newlabDipChartModel = new LabdipChartModel()
                {   Index = index, 
                    Division = _labdipChartModel.Division,
                    Season = _labdipChartModel.Season,
                    Category = _labdipChartModel.Category,
                    Program = null,
                    PackCombination = null,
                    StyleNoIndividual = _labdipChartModel.StyleNoIndividual,
                    GMTDescription = _labdipChartModel.GMTDescription,
                    GMTColor = row.Cells[0].ColValue,
                    RMColor = row.Cells[1].ColValue,
                    PalcementName = row.Cells[7].ColValue,
                    BOMSelection = row.Cells[8].ColValue,
                    ItemName = row.Cells[5].ColValue,
                    SupplierName = row.Cells[6].ColValue,
                    MaterialType = row.Cells[4].ColValue,
                    FBNumber =  ExtractFBNumber(row.Cells[4].ColValue), //row.Cells[3].ColValue 
                    GarmentWay = FilterGarmentWay(row.Cells[2].ColValue, row.Cells[4].ColValue, row.Cells[1].ColValue, labdipChartModels,index, row.Cells[7].ColValue)
                };
                labdipChartModels.Add(newlabDipChartModel);
                index++;
            });
            return labdipChartModels;
        }

        private byte[] FileToByteArray(string fileName)
        {
            byte[] fileContent = null;
            System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(fs);
            long byteLength = new System.IO.FileInfo(fileName).Length;
            fileContent = binaryReader.ReadBytes((Int32)byteLength);
            fs.Close();
            fs.Dispose();
            binaryReader.Close();
            return fileContent;
        }

        private string NullReplacement(string stringVal)
        {
            return string.IsNullOrEmpty(stringVal) ? null : stringVal.Equals("Null") ? null : stringVal;
        }

        private string SetSectionData(Row headerRow, Row currentRow, string[] headerNameList)
        {
            string result = null;

            for (int i = 0; i < headerRow.Cells.Count; i++)
            {
               if(Array.Exists(headerNameList,s=> s.Equals(headerRow.Cells[i].ColValue)))
                    result = currentRow.Cells[i].ColValue;

                //if (headerRow.Cells[i].ColValue == headerName)
                //result = currentRow.Cells[i].ColValue;
            }
            return result;
        }

        private string ExtractFBNumber(string material)
        {
            var stack = new Stack<char>();

            foreach (var c in ReverseString(material))
            {
                if (!char.IsDigit(c))
                    break;
                stack.Push(c);
            }

            return new string(stack.ToArray());
        }

        private string  ReverseString(string stringInput)
        {
            // With Inbuilt Method Array.Reverse Method  
            char[] charArray = stringInput.ToCharArray();
            Array.Reverse(charArray);
           return (new string(charArray));
        }

        private string FilterGarmentWay(string partType,string material, string colorWayCol, List<LabdipChartModel> labdipChartModels,int currentIndex,string placementName )
        {
            string result = "";
            if(!string.IsNullOrEmpty(material) && !string.IsNullOrEmpty(partType))
                result = (partType.ToLower().Contains("comments") && material.ToLower().Contains("print")) ? UpdatePreviousGarmentwayColumns(currentIndex,
                                                                                                                                             labdipChartModels,
                                                                                                                                             placementName,
                                                                                                                                             colorWayCol) : "";
            return result;
        }

        private string  UpdatePreviousGarmentwayColumns(int index, List<LabdipChartModel> labdipChartModels, string placementName,string colorWay)
        {
            bool notFound = true;
            int rowIndex = index-1;
            while (notFound && rowIndex > 0)
            {                
                rowIndex--;
                if (labdipChartModels[rowIndex].PalcementName.Equals(placementName))
                {
                    labdipChartModels[rowIndex].GarmentWay = colorWay;
                }
                else
                {
                    notFound = false;
                }
            }
            return colorWay;
        }
    }
}
