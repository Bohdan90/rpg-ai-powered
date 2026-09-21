using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("RPG.Core")]
// Tests may prepare spent-movement snapshots without implementing a Move command in M1.
[assembly: InternalsVisibleTo("RPG.Tests")]
