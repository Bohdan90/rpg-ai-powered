using System.Reflection;
using NUnit.Framework;

namespace RPG.Tests
{
    public class CoreArchitectureTests
    {
        [Test]
        public void CoreDoesNotReferenceUnityOrPresentation()
        {
            var core = Assembly.Load("RPG.Core");

            foreach (var reference in core.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Does.Not.StartWith("Unity"),
                    "Core must remain independent of Unity assemblies.");
                Assert.That(reference.Name, Is.Not.EqualTo("RPG.Presentation"),
                    "Core must remain independent of Presentation.");
            }
        }
    }
}
