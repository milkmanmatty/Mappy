namespace MappyTest
{
    using System.Collections.Generic;

    using Mappy.Util;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InitialMissionCommandsTest
    {
        [TestMethod]
        public void SplitJoinRoundTripsATypicalMission()
        {
            const string Mission = "o 0 1,w 60,p 807 1632";

            var commands = InitialMissionCommands.Split(Mission);

            Assert.AreEqual(3, commands.Count);
            Assert.AreEqual("o 0 1", commands[0]);
            Assert.AreEqual("w 60", commands[1]);
            Assert.AreEqual("p 807 1632", commands[2]);
            Assert.AreEqual(Mission, InitialMissionCommands.Join(commands));
        }

        [TestMethod]
        public void SplitPreservesUnknownOpcodes()
        {
            var commands = InitialMissionCommands.Split("xyz whatever,o 2 2");

            Assert.AreEqual(2, commands.Count);
            Assert.AreEqual("xyz whatever", commands[0]);
            Assert.AreEqual("o 2 2", commands[1]);
            Assert.AreEqual("xyz whatever,o 2 2", InitialMissionCommands.Join(commands));
        }

        [TestMethod]
        public void SplitTrimsSegmentsAndDropsEmpties()
        {
            var commands = InitialMissionCommands.Split("  m 10 20 , ,\tw 5  ,");

            Assert.AreEqual(2, commands.Count);
            Assert.AreEqual("m 10 20", commands[0]);
            Assert.AreEqual("w 5", commands[1]);
        }

        [TestMethod]
        public void SplitReturnsEmptyForBlankInput()
        {
            Assert.AreEqual(0, InitialMissionCommands.Split(null).Count);
            Assert.AreEqual(0, InitialMissionCommands.Split(string.Empty).Count);
            Assert.AreEqual(0, InitialMissionCommands.Split("   ").Count);
            Assert.AreEqual(0, InitialMissionCommands.Split(",,,").Count);
        }

        [TestMethod]
        public void JoinSkipsBlankCommands()
        {
            var joined = InitialMissionCommands.Join(new List<string> { "m 1 2", "  ", null, " s " });

            Assert.AreEqual("m 1 2,s", joined);
        }

        [TestMethod]
        public void JoinReturnsEmptyForNull()
        {
            Assert.AreEqual(string.Empty, InitialMissionCommands.Join(null));
        }
    }
}
