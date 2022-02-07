using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateModel
{
    [Transaction(TransactionMode.Manual)]

    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = GetLevelByName(doc, "Уровень 1");
            Level level2 = GetLevelByName(doc, "Уровень 2");

            CreateWalls(doc, 10000, 5000, level1, level2, false);


            return Result.Succeeded;
        }


        public Level GetLevelByName (Document doc, string name)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
               .OfClass(typeof(Level))
               .OfType<Level>()
               .ToList();

            Level level = listLevel
                .Where(x => x.Name.Equals(name))
                .FirstOrDefault();
            return level;
        }
        public void CreateWalls(Document doc, double width, double depth, Level levelDown, Level levelHeight, bool structural)
        {
            double _width = UnitUtils.ConvertToInternalUnits(width, UnitTypeId.Millimeters);
            double _depth = UnitUtils.ConvertToInternalUnits(depth, UnitTypeId.Millimeters);
            double dx = _width / 2;
            double dy = _depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, levelDown.Id, structural);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(levelHeight.Id);
            }
            transaction.Commit();
        }
    }
}
