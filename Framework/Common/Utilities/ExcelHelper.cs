using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MECF.Framework.Common.Utilities
{
	public class ExcelHelper
	{
		public static bool ExportToExcel(string filepath, DataSet ds, out string reason, bool createNewFile = true)
		{
			reason = string.Empty;

			try
			{
				SpreadsheetDocument spreadsheetDocument;
				WorkbookPart workbookpart;
				WorksheetPart worksheetPart;
				Sheets sheets;
				if (createNewFile)
				{
					spreadsheetDocument = SpreadsheetDocument.Create(filepath, SpreadsheetDocumentType.Workbook);
					workbookpart = spreadsheetDocument.AddWorkbookPart();
					workbookpart.Workbook = new Workbook();
					sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
				}
				else
				{
					spreadsheetDocument = SpreadsheetDocument.Open(filepath, true);
					workbookpart = spreadsheetDocument.WorkbookPart;
					sheets = spreadsheetDocument.WorkbookPart.Workbook.Sheets;
				}
				worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
				worksheetPart.Worksheet = new Worksheet(new SheetData());

				uint sheetID = 1;
				if (sheets.Descendants<Sheet>().Count() > 0)
				{
					sheetID = sheets.Descendants<Sheet>().Select(S => S.SheetId.Value).Max() + 1;
				}

				for (int i = 0; i < ds.Tables.Count; i++)
				{
					Sheet sheet = new Sheet()
					{
						Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
						SheetId = sheetID,
						Name = ds.Tables[i].TableName,
					};

					sheets.Append(sheet);

					var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

					Row rowCaption = new Row();
					for (int col = 0; col < ds.Tables[i].Columns.Count; col++)
					{
						Cell titleCell = new Cell();
						titleCell.CellValue = new CellValue(ds.Tables[i].Columns[col].Caption);
						titleCell.DataType = new EnumValue<CellValues>(CellValues.String);
                        rowCaption.Append(titleCell);
					}
					sheetData.Append(rowCaption);

					for (int row = 0; row < ds.Tables[i].Rows.Count; row++)
					{
						Row rowData = new Row();
						for (int col = 0; col < ds.Tables[i].Columns.Count; col++)
						{
							Cell dataCell = new Cell();

							var data = ds.Tables[i].Rows[row][col];

							if (data is DateTime time)
							{
								dataCell.CellValue = new CellValue(time);
								dataCell.DataType = new EnumValue<CellValues>(CellValues.String);
							}
							else if (data is double value)
							{
								dataCell.CellValue = new CellValue(data.ToString());
								dataCell.DataType = new EnumValue<CellValues>(CellValues.Number);
							}
							else
							{
								dataCell.CellValue = new CellValue(data.ToString());
								dataCell.DataType = new EnumValue<CellValues>(CellValues.String);
							}

							rowData.Append(dataCell);
						}
						sheetData.Append(rowData);
					}
				}
				workbookpart.Workbook.Save();
				spreadsheetDocument.Close();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				reason = ex.Message;
				return false;
			}

			return true;

		}

	}
}
