using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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


            List<Wall> walls = CreateWalls(doc, 10000, 5000, level1, level2, false);

            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1]);
            AddWindow(doc, level1, walls[2]);
            AddWindow(doc, level1, walls[3]);
            AddRoof(doc, level2, walls);

            return Result.Succeeded;
        }

        private void AddRoof(Document doc, Level level, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))                
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            CurveArray curveArray = new CurveArray();
            
            XYZ point1 = (walls[1].Location as LocationCurve).Curve.GetEndPoint(0);
            XYZ point2 = (walls[1].Location as LocationCurve).Curve.GetEndPoint(1);
            XYZ point3 = (point2 + point1) / 2;
            
            double z = level.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble();
            double dt = walls[1].Width;

            curveArray.Append(Line.CreateBound(new XYZ(point1.X, point1.Y - dt, z ), new XYZ(point3.X, point3.Y, z+10)));
            curveArray.Append(Line.CreateBound(new XYZ(point3.X, point3.Y, z+10), new XYZ(point2.X, point2.Y + dt, z)));

            double extrusionStart = (walls[0].Location as LocationCurve).Curve.GetEndPoint(0).X;
            double extrusionEnd = (walls[0].Location as LocationCurve).Curve.GetEndPoint(1).X;

            Transaction transaction = new Transaction(doc, "Добавление крыши");
            transaction.Start();
            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level, roofType, extrusionStart - dt, extrusionEnd + dt);
            transaction.Commit();
        }

        private void AddWindow(Document doc, Level level, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0610 x 1220 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            Transaction transaction = new Transaction(doc, "Добавление окна");
            transaction.Start();
            if (!windowType.IsActive)
            {
                windowType.Activate();
            }
            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level, StructuralType.NonStructural);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(UnitUtils.ConvertToInternalUnits(800, UnitTypeId.Millimeters));
            transaction.Commit();
            
        }

        private void AddDoor(Document doc, Level level, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();
            LocationCurve hostCurve =  wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            Transaction transaction = new Transaction(doc, "Добавление двери");
            transaction.Start();
            if (!doorType.IsActive)
                 doorType.Activate();            
            doc.Create.NewFamilyInstance(point, doorType, wall, level, StructuralType.NonStructural);
            
            transaction.Commit();            

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
        public List<Wall> CreateWalls(Document doc, double width, double depth, Level levelDown, Level levelHeight, bool structural)
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
            return walls;
        }
    }
}
