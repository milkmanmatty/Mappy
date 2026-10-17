namespace MappyTest
{
    using System;
    using System.IO;
    using System.Text;

    using Mappy.Data;
    using Mappy.Models;
    using Mappy.Operations;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TAUtil.Tdf;

    [TestClass]
    public class SchemaAttributesRoundTripTest
    {
        [TestMethod]
        public void OtaRoundTripKeepsSchemaFieldsOnEachSchema()
        {
            var attrs = new MapAttributes();
            var hard = attrs.AddSchema("Hard");

            attrs.Schemas[0].SchemaType = "Network 1";
            attrs.Schemas[0].AiProfile = "EASY";
            attrs.Schemas[0].SurfaceMetal = 4;
            attrs.Schemas[0].MohoMetal = 40;
            attrs.Schemas[0].HumanMetal = 1100;
            attrs.Schemas[0].ComputerMetal = 1200;
            attrs.Schemas[0].HumanEnergy = 1300;
            attrs.Schemas[0].ComputerEnergy = 1400;
            attrs.Schemas[0].MeteorWeapon = "meteor1";
            attrs.Schemas[0].MeteorRadius = 10;
            attrs.Schemas[0].MeteorDensity = 1.5;
            attrs.Schemas[0].MeteorDuration = 20;
            attrs.Schemas[0].MeteorInterval = 30;

            hard.AiProfile = "HARD";
            hard.SurfaceMetal = 9;
            hard.MohoMetal = 90;
            hard.HumanMetal = 2100;
            hard.ComputerMetal = 2200;
            hard.HumanEnergy = 2300;
            hard.ComputerEnergy = 2400;
            hard.MeteorWeapon = "meteor2";
            hard.MeteorRadius = 11;
            hard.MeteorDensity = 2.5;
            hard.MeteorDuration = 21;
            hard.MeteorInterval = 31;

            MapAttributes loaded;
            string text;
            using (var stream = new MemoryStream())
            {
                attrs.WriteOta(stream, 2, 2);
                text = Encoding.ASCII.GetString(stream.ToArray());
                stream.Position = 0;
                loaded = MapAttributes.Load(TdfNode.LoadTdf(stream));
            }

            Assert.IsFalse(loaded.GlobalHeaderExtraEntries.ContainsKey("SurfaceMetal"));
            Assert.IsFalse(loaded.GlobalHeaderExtraEntries.ContainsKey("aiprofile"));
            Assert.IsTrue(text.IndexOf("[Schema 0]", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.IsTrue(text.IndexOf("[Schema 1]", StringComparison.OrdinalIgnoreCase) >= 0);

            AssertSchema(loaded.Schemas[0], "EASY", 4, 40, 1100, 1200, 1300, 1400, "meteor1", 10, 1.5, 20, 30);
            AssertSchema(loaded.Schemas[1], "HARD", 9, 90, 2100, 2200, 2300, 2400, "meteor2", 11, 2.5, 21, 31);
        }

        [TestMethod]
        public void MergeIntoAndUndoRestoreEverySchema()
        {
            var attrs = new MapAttributes();
            attrs.AddSchema("Hard");
            attrs.Schemas[0].AiProfile = "EASY";
            attrs.Schemas[0].HumanMetal = 1000;
            attrs.Schemas[1].AiProfile = "HARD";
            attrs.Schemas[1].HumanMetal = 1000;
            attrs.Schemas[1].MeteorWeapon = "meteor2";

            var map = new MapModel(1, 1, attrs);
            map.ActiveSchemaIndex = 1;

            var edited = MapAttributesResult.FromModel(map);
            Assert.AreEqual(2, edited.Schemas.Count);
            Assert.AreEqual("HARD", edited.Schemas[1].AiProfile);

            edited.Name = "Renamed";
            edited.Schemas[0].AiProfile = "EASY2";
            edited.Schemas[0].SurfaceMetal = 7;
            edited.Schemas[1].HumanMetal = 50;
            edited.Schemas[1].ComputerEnergy = 60;
            edited.Schemas[1].MeteorInterval = 8;
            edited.Schemas[1].SchemaType = "SHOULD_NOT_APPLY";

            var operation = new ChangeAttributesOperation(map, edited);
            operation.Execute();

            Assert.AreEqual("Renamed", map.Attributes.Name);
            Assert.AreEqual("EASY2", map.Attributes.Schemas[0].AiProfile);
            Assert.AreEqual(7, map.Attributes.Schemas[0].SurfaceMetal);
            Assert.AreEqual(50, map.Attributes.Schemas[1].HumanMetal);
            Assert.AreEqual(60, map.Attributes.Schemas[1].ComputerEnergy);
            Assert.AreEqual(8, map.Attributes.Schemas[1].MeteorInterval);
            Assert.AreEqual("Hard", map.Attributes.Schemas[1].SchemaType);

            operation.Undo();

            Assert.AreEqual("Untitled Map", map.Attributes.Name);
            Assert.AreEqual("EASY", map.Attributes.Schemas[0].AiProfile);
            Assert.AreEqual(3, map.Attributes.Schemas[0].SurfaceMetal);
            Assert.AreEqual(1000, map.Attributes.Schemas[1].HumanMetal);
            Assert.AreEqual(1000, map.Attributes.Schemas[1].ComputerEnergy);
            Assert.AreEqual(0, map.Attributes.Schemas[1].MeteorInterval);
            Assert.AreEqual("meteor2", map.Attributes.Schemas[1].MeteorWeapon);
        }

        private static void AssertSchema(
            MapSchema schema,
            string aiProfile,
            int surfaceMetal,
            int mohoMetal,
            int humanMetal,
            int computerMetal,
            int humanEnergy,
            int computerEnergy,
            string meteorWeapon,
            int meteorRadius,
            double meteorDensity,
            int meteorDuration,
            int meteorInterval)
        {
            Assert.AreEqual(aiProfile, schema.AiProfile);
            Assert.AreEqual(surfaceMetal, schema.SurfaceMetal);
            Assert.AreEqual(mohoMetal, schema.MohoMetal);
            Assert.AreEqual(humanMetal, schema.HumanMetal);
            Assert.AreEqual(computerMetal, schema.ComputerMetal);
            Assert.AreEqual(humanEnergy, schema.HumanEnergy);
            Assert.AreEqual(computerEnergy, schema.ComputerEnergy);
            Assert.AreEqual(meteorWeapon, schema.MeteorWeapon);
            Assert.AreEqual(meteorRadius, schema.MeteorRadius);
            Assert.AreEqual(meteorDensity, schema.MeteorDensity, 0.001);
            Assert.AreEqual(meteorDuration, schema.MeteorDuration);
            Assert.AreEqual(meteorInterval, schema.MeteorInterval);
        }
    }
}
