namespace MappyTest
{
    using System;
    using System.Collections.Generic;

    using Mappy.Data;
    using Mappy.Util;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InitialMissionPathTest
    {
        [TestMethod]
        public void MoveDrawsASegmentToTheDestination()
        {
            var route = InitialMissionPath.Build("m 30 40", 0, 0, Guid.NewGuid(), null);

            Assert.AreEqual(1, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Move, route.Segments[0].Kind);
            Assert.AreEqual(0, route.Segments[0].X1);
            Assert.AreEqual(0, route.Segments[0].Z1);
            Assert.AreEqual(30, route.Segments[0].X2);
            Assert.AreEqual(40, route.Segments[0].Z2);
            Assert.AreEqual(0, route.Waits.Count);
        }

        [TestMethod]
        public void TrailingPatrolClosesTheLoopBackToTheFirstPatrolPoint()
        {
            var route = InitialMissionPath.Build("p 10 0, p 10 10, p 0 10", 0, 0, Guid.NewGuid(), null);

            Assert.AreEqual(4, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Patrol, route.Segments[0].Kind);
            Assert.AreEqual(10, route.Segments[0].X2);
            Assert.AreEqual(0, route.Segments[0].Z2);
            Assert.AreEqual(0, route.Segments[3].X1);
            Assert.AreEqual(10, route.Segments[3].Z1);
            Assert.AreEqual(10, route.Segments[3].X2);
            Assert.AreEqual(0, route.Segments[3].Z2);
        }

        [TestMethod]
        public void SingleTrailingPatrolIsAThereAndBackOnTheSameSegment()
        {
            var route = InitialMissionPath.Build("p 10 20", 1, 2, Guid.NewGuid(), null);

            Assert.AreEqual(1, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Patrol, route.Segments[0].Kind);
            Assert.AreEqual(1, route.Segments[0].X1);
            Assert.AreEqual(2, route.Segments[0].Z1);
            Assert.AreEqual(10, route.Segments[0].X2);
            Assert.AreEqual(20, route.Segments[0].Z2);
        }

        [TestMethod]
        public void PatrolFollowedByAMoveDoesNotCloseTheLoop()
        {
            var route = InitialMissionPath.Build("p 10 10, m 20 20", 0, 0, Guid.NewGuid(), null);

            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Patrol, route.Segments[0].Kind);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Move, route.Segments[1].Kind);
            Assert.AreEqual(20, route.Segments[1].X2);
            Assert.AreEqual(20, route.Segments[1].Z2);
        }

        [TestMethod]
        public void AttackByNameGoesToTheNearestMatchingUnit()
        {
            var self = new SchemaUnit(Guid.NewGuid(), "ARMFLASH") { XPos = 0, ZPos = 0 };
            var near = new SchemaUnit(Guid.NewGuid(), "ARMZEUS") { XPos = 100, ZPos = 0 };
            var far = new SchemaUnit(Guid.NewGuid(), "armzeus") { XPos = 500, ZPos = 40 };
            var route = InitialMissionPath.Build("a ARMZEUS", 0, 0, self.Id, new[] { self, far, near });

            Assert.AreEqual(1, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Attack, route.Segments[0].Kind);
            Assert.AreEqual(100, route.Segments[0].X2);
            Assert.AreEqual(0, route.Segments[0].Z2);
        }

        [TestMethod]
        public void AttackByIdentGoesToThatUnit()
        {
            var self = new SchemaUnit(Guid.NewGuid(), "CORAPE") { XPos = 0, ZPos = 0 };
            var trap = new SchemaUnit(Guid.NewGuid(), "CORRAD")
            {
                Ident = "TRAP",
                XPos = 40,
                ZPos = 70,
            };
            var route = InitialMissionPath.Build("a trap", 0, 0, self.Id, new[] { self, trap });

            Assert.AreEqual(1, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Attack, route.Segments[0].Kind);
            Assert.AreEqual(40, route.Segments[0].X2);
            Assert.AreEqual(70, route.Segments[0].Z2);
        }

        [TestMethod]
        public void AttackByCoordinateDrawsToThatPoint()
        {
            var route = InitialMissionPath.Build("a 513 700", 0, 0, Guid.NewGuid(), null);

            Assert.AreEqual(1, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Attack, route.Segments[0].Kind);
            Assert.AreEqual(513, route.Segments[0].X2);
            Assert.AreEqual(700, route.Segments[0].Z2);
        }

        [TestMethod]
        public void MissingAttackTargetDrawsNothing()
        {
            var self = new SchemaUnit(Guid.NewGuid(), "ARMFLASH");
            var route = InitialMissionPath.Build("a ARMSPID", 0, 0, self.Id, new[] { self });

            Assert.AreEqual(0, route.Segments.Count);
        }

        [TestMethod]
        public void BuildAtPointIsAMoveAndLabBuildIsNot()
        {
            var built = InitialMissionPath.Build("b corllt 1 890 3000", 0, 0, Guid.NewGuid(), null);
            Assert.AreEqual(1, built.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Move, built.Segments[0].Kind);
            Assert.AreEqual(890, built.Segments[0].X2);
            Assert.AreEqual(3000, built.Segments[0].Z2);

            var lab = InitialMissionPath.Build("b armvp 5", 0, 0, Guid.NewGuid(), null);
            Assert.AreEqual(0, lab.Segments.Count);
            Assert.AreEqual(0, lab.Waits.Count);
        }

        [TestMethod]
        public void WaitAtSpawnAndWaitAfterAMove()
        {
            var atSpawn = InitialMissionPath.Build("w 200", 15, 25, Guid.NewGuid(), null);
            Assert.AreEqual(0, atSpawn.Segments.Count);
            Assert.AreEqual(1, atSpawn.Waits.Count);
            Assert.AreEqual(15, atSpawn.Waits[0].X);
            Assert.AreEqual(25, atSpawn.Waits[0].Z);
            Assert.AreEqual(200, atSpawn.Waits[0].Seconds);
            Assert.AreEqual(0, atSpawn.Waits[0].StackIndex);

            var afterMove = InitialMissionPath.Build("m 80 90, w 12", 0, 0, Guid.NewGuid(), null);
            Assert.AreEqual(1, afterMove.Segments.Count);
            Assert.AreEqual(80, afterMove.Waits[0].X);
            Assert.AreEqual(90, afterMove.Waits[0].Z);
            Assert.AreEqual(12, afterMove.Waits[0].Seconds);
        }

        [TestMethod]
        public void WaitsAtTheSamePointStackAndWaitUntilAttackedHasNoBox()
        {
            var route = InitialMissionPath.Build("w 5, wa TRAP, w 9", 1, 2, Guid.NewGuid(), null);

            Assert.AreEqual(2, route.Waits.Count);
            Assert.AreEqual(5, route.Waits[0].Seconds);
            Assert.AreEqual(0, route.Waits[0].StackIndex);
            Assert.AreEqual(9, route.Waits[1].Seconds);
            Assert.AreEqual(1, route.Waits[1].StackIndex);
            Assert.AreEqual(route.Waits[0].X, route.Waits[1].X);
            Assert.AreEqual(route.Waits[0].Z, route.Waits[1].Z);
        }

        [TestMethod]
        public void GuardDrawsAMoveToTheIdent()
        {
            var self = new SchemaUnit(Guid.NewGuid(), "CORCK") { XPos = 0, ZPos = 0 };
            var target = new SchemaUnit(Guid.NewGuid(), "CORCOM")
            {
                Ident = "DAXOS",
                XPos = 12,
                ZPos = 34,
            };
            var route = InitialMissionPath.Build("g daxos", 0, 0, self.Id, new List<SchemaUnit> { self, target });

            Assert.AreEqual(1, route.Segments.Count);
            Assert.AreEqual(InitialMissionPath.SegmentKind.Move, route.Segments[0].Kind);
            Assert.AreEqual(12, route.Segments[0].X2);
            Assert.AreEqual(34, route.Segments[0].Z2);
        }

        [TestMethod]
        public void ConstructorExampleWaitsThenBuilds()
        {
            var route = InitialMissionPath.Build(
                "wa NWTRIG,w 120,b corllt 1 890 3000,b corrad 1 830 2900",
                1481,
                4218,
                Guid.NewGuid(),
                null);

            Assert.AreEqual(1, route.Waits.Count);
            Assert.AreEqual(120, route.Waits[0].Seconds);
            Assert.AreEqual(1481, route.Waits[0].X);
            Assert.AreEqual(4218, route.Waits[0].Z);
            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(890, route.Segments[0].X2);
            Assert.AreEqual(3000, route.Segments[0].Z2);
            Assert.AreEqual(830, route.Segments[1].X2);
            Assert.AreEqual(2900, route.Segments[1].Z2);
        }
    }
}
