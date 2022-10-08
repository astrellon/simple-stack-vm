using System;
using System.IO;
using System.Linq;

namespace SimpleStackVM
{
    public static class StandardLibraryProgram
    {
        #region Methods
        public static void Main(string[] args)
        {
            var json = SimpleJSON.JSON.Parse(File.ReadAllText("../examples/testStandardLibrary.json"));
            var scopes = VirtualMachineJsonAssembler.ParseProcedures(json.AsArray);

            // var vm = new VirtualMachine(64, OnRunCommand);
            // StandardLibrary.AddToVirtualMachine(vm, StandardLibrary.LibraryType.All);
            // StandardAssertLibrary.AddHandler(vm);
            // // vm.AddProcedures(scopes);

            // try
            // {
            //     // vm.SetCurrentProcedure("Test1");
            //     vm.Running = true;
            //     while (vm.Running && !vm.Paused)
            //     {
            //         vm.Step();
            //     }
            // }
            // catch (VirtualMachineException exp)
            // {
            //     Console.WriteLine(exp.Message);
            //     var stackTrace = string.Join("", exp.VirtualMachineStackTrace.Select(t => $"\n- {t}"));
            //     Console.WriteLine($"VM Stack: {stackTrace}");
            //     Console.WriteLine(exp.StackTrace);
            // }
        }

        private static void OnRunCommand(string command, VirtualMachine vm)
        {
            if (command == "print")
            {
                var top = vm.PopStack();
                Console.WriteLine($"Print: {top.ToString()}");
            }
        }
        #endregion
    }
}