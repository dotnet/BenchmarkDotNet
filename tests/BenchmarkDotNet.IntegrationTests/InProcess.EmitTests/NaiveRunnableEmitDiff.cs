﻿using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests
{
    public class NaiveRunnableEmitDiff
    {
        private static readonly HashSet<string> IgnoredTypeNames = new HashSet<string>()
        {
            "BenchmarkDotNet.Autogenerated.UniqueProgramName",
            "BenchmarkDotNet.Autogenerated.DirtyAssemblyResolveHelper" // not required to be used in the InProcess toolchains (it's already used in the host process)
        };

        private static readonly HashSet<string> IgnoredAttributeTypeNames = new HashSet<string>()
        {
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute"
        };

        private static readonly HashSet<string> IgnoredRunnableMethodNames = new HashSet<string>()
        {
            "Run",
            ".ctor"
        };

        private static readonly IReadOnlyDictionary<OpCode, OpCode> AltOpCodes = new Dictionary<OpCode, OpCode>()
        {
            { OpCodes.Br_S, OpCodes.Br },
            { OpCodes.Blt_S, OpCodes.Blt },
            { OpCodes.Bne_Un_S, OpCodes.Bne_Un },
            { OpCodes.Bge_S, OpCodes.Bge },
            { OpCodes.Brtrue_S, OpCodes.Brtrue },
        };

        public static void RunDiff(string roslynAssemblyPath, string emittedAssemblyPath, ILogger logger)
        {
            using (var roslynAssemblyDefinition = AssemblyDefinition.ReadAssembly(roslynAssemblyPath))
            using (var emittedAssemblyDefinition = AssemblyDefinition.ReadAssembly(emittedAssemblyPath))
            {
                Diff(roslynAssemblyDefinition, emittedAssemblyDefinition, logger);
            }

            // all checks have passed, so we can remove the files to avoid "file in use" problems
            System.IO.File.Delete(roslynAssemblyPath);
            System.IO.File.Delete(emittedAssemblyPath);
        }

        private static bool IsRunnable(TypeReference t) =>
            t.FullName.StartsWith("BenchmarkDotNet.Autogenerated.Runnable_");

        private static bool AreSameTypeIgnoreNested(TypeReference left, TypeReference right)
        {
            if (left == null)
                return right == null;
            if (right == null)
                return false;

            return left.FullName.Replace("/", "").Replace(".ReplaceMe.", ".") ==
                   right.FullName.Replace("/", "").Replace(".ReplaceMe.", ".");
        }

        private static bool AreSameSignature(MethodReference left, MethodReference right)
        {
            var lookup = new HashSet<string>()
            {
                RunnableConstants.WorkloadImplementationMethodName,
                RunnableConstants.GlobalSetupMethodName,
                RunnableConstants.GlobalCleanupMethodName,
                RunnableConstants.IterationSetupMethodName,
                RunnableConstants.IterationCleanupMethodName
            };
            return (left.Name == right.Name || (left.Name.StartsWith("<.ctor>") && lookup.Contains(right.Name)))
                && AreSameTypeIgnoreNested(left.ReturnType, right.ReturnType)
                && left.Parameters.Count == right.Parameters.Count
                && left.Parameters
                    .Zip(right.Parameters, (p1, p2) => (p1, p2))
                    .All(p => AreSameTypeIgnoreNested(p.p1.ParameterType, p.p2.ParameterType));
        }

        private static List<Instruction> GetOpInstructions(MethodDefinition method)
        {
            var bodyInstructions = method.Body.GetILProcessor().Body.Instructions;

            // There's something wrong with ldloc/ldarg with index >= 255. The c# compiler emits random nops for them.
            var compareNops = method.Body.Variables.Count < 255 && method.Parameters.Count < 255;
            var result = new List<Instruction>(bodyInstructions.Count);
            foreach (var instruction in bodyInstructions)
            {
                // Skip leave instructions since the IlBuilder forces them differently than Roslyn.
                if (instruction.OpCode != OpCodes.Leave && instruction.OpCode != OpCodes.Leave_S
                    && (compareNops || instruction.OpCode != OpCodes.Nop))
                    result.Add(instruction);
            }

            return result;
        }

        private static void DiffSignature(TypeReference left, TypeReference right)
        {
            if (right == null)
                throw new InvalidOperationException($"No matching type for {left}");

            if (!AreSameTypeIgnoreNested(left, right))
                throw new InvalidOperationException($"No matching type for {left}");
        }

        private static void DiffSignature(FieldReference left, FieldReference right)
        {
            if (right == null)
                throw new InvalidOperationException($"No matching field for {left.FullName}");

            if (!AreSameTypeIgnoreNested(left.FieldType, right.FieldType))
                throw new InvalidOperationException($"No matching field for {left.FullName}");

            if (!AreSameTypeIgnoreNested(left.DeclaringType, right.DeclaringType))
                throw new InvalidOperationException($"No matching field for {left.FullName}");
        }

        private static void DiffSignature(MethodReference left, MethodReference right)
        {
            if (right == null)
                throw new InvalidOperationException($"No matching method for {left}");

            if (!AreSameSignature(left, right))
                throw new InvalidOperationException($"No matching method for {left}");

            if (!AreSameTypeIgnoreNested(left.DeclaringType, right.DeclaringType))
                throw new InvalidOperationException($"No matching method for {left}");
        }

        private static void DiffSignature(ParameterDefinition left, ParameterDefinition right)
        {
            if (left.Name != right.Name)
                throw new InvalidOperationException($"No matching parameter for {left.Name} ({left.Method})");

            if (!AreSameTypeIgnoreNested(left.ParameterType, right.ParameterType))
                throw new InvalidOperationException($"No matching parameter for {left.Name} ({left.Method})");

            if (left.Attributes != right.Attributes)
                throw new InvalidOperationException($"No matching parameter for {left.Name} ({left.Method})");
        }

        private static void DiffSignature(Instruction left, Instruction right, MethodDefinition method)
        {
            if (left.OpCode != right.OpCode)
            {
                if (!AltOpCodes.TryGetValue(left.OpCode, out var altOpCode1) || altOpCode1 != right.OpCode)
                    throw new InvalidOperationException($"No matching op for {left} ({method}).");
            }
            else if (left.GetSize() != right.GetSize())
            {
                throw new InvalidOperationException($"No matching op for {left} ({method}).");
            }

            if (left.Operand == null && right.Operand != null)
                throw new InvalidOperationException($"No matching op for {left} ({method}).");

            if (left.Operand != null && right.Operand == null)
                throw new InvalidOperationException($"No matching op for {left} ({method}).");
        }

        private static void DiffSignature(VariableDefinition left, VariableDefinition right, MethodDefinition method)
        {
            if (left.Index != right.Index)
                throw new InvalidOperationException($"No matching variable for {left} ({method}).");

            if (left.IsPinned != right.IsPinned)
                throw new InvalidOperationException($"No matching variable for {left} ({method}).");

            if (!AreSameTypeIgnoreNested(left.VariableType, right.VariableType))
                throw new InvalidOperationException($"No matching variable for {left} ({method}).");
        }

        private static void Diff(
            Collection<CustomAttribute> left,
            Collection<CustomAttribute> right,
            ICustomAttributeProvider owner)
        {
            var attributes2ByTypeName = right.ToLookup(a => a.AttributeType.FullName);
            foreach (var attribute1 in left)
            {
                var attribute2 = attributes2ByTypeName[attribute1.AttributeType.FullName].FirstOrDefault();
                Diff(attribute1, attribute2, owner);
            }
        }

        private static void Diff(CustomAttribute left, CustomAttribute right, ICustomAttributeProvider owner)
        {
            if (IgnoredAttributeTypeNames.Contains(left.AttributeType.FullName) && right == null)
                return;

            if (right == null)
                throw new InvalidOperationException($"No matching attribute for {left.AttributeType} ({owner})");

            if (!AreSameTypeIgnoreNested(left.AttributeType, right.AttributeType))
                throw new InvalidOperationException($"No matching attribute for {left.AttributeType} ({owner})");

            if (left.ConstructorArguments.Count != right.ConstructorArguments.Count)
                throw new InvalidOperationException($"No matching attribute for {left.AttributeType} ({owner})");

            for (int i = 0; i < left.ConstructorArguments.Count; i++)
            {
                var attArg1 = left.ConstructorArguments[i];
                var attArg2 = right.ConstructorArguments[i];

                if (!AreSameTypeIgnoreNested(attArg1.Type, attArg2.Type))
                    throw new InvalidOperationException($"No matching attribute for {left.AttributeType} ({owner})");

                if (!Equals(attArg1.Value, attArg2.Value))
                    throw new InvalidOperationException($"No matching attribute for {left.AttributeType} ({owner})");
            }
        }

        private static void Diff(
            AssemblyDefinition roslynAssemblyDefinition,
            AssemblyDefinition emittedAssemblyDefinition,
            ILogger logger)
        {
            Diff(roslynAssemblyDefinition.CustomAttributes, emittedAssemblyDefinition.CustomAttributes, roslynAssemblyDefinition);

            var modules2ByName = emittedAssemblyDefinition.Modules.ToLookup(m => m.Name);
            foreach (var module1 in roslynAssemblyDefinition.Modules)
            {
                var module2 = modules2ByName[module1.Name].SingleOrDefault();
                if (module2 == null && module1.IsMain)
                    module2 = emittedAssemblyDefinition.MainModule;

                Diff(module1, module2, logger);
            }
        }

        private static void Diff(ModuleDefinition module1, ModuleDefinition module2, ILogger logger)
        {
            Diff(module1.CustomAttributes, module2.CustomAttributes, module1);

            foreach (var type1 in module1.Types)
            {
                var type2 = module2.Types
                    .SingleOrDefault(t => AreSameTypeIgnoreNested(type1, t));

                Diff(type1, type2, logger);
            }
        }

        private static void Diff(TypeDefinition type1, TypeDefinition type2, ILogger logger)
        {
            try
            {
                logger.WriteStatistic($"Diff {type1.FullName}");

                if (IgnoredTypeNames.Contains(type1.FullName) && type2 == null)
                {
                    logger.WriteLineInfo(" SKIPPED.");
                    return;
                }

                logger.WriteLine();

                DiffDefinition(type1, type2);

                DiffMembers(type1, type2, logger);
            }
            catch (Exception ex)
            {
                logger.WriteLineError(ex.ToString());
                throw;
            }
        }

        private static void DiffDefinition(TypeDefinition type1, TypeDefinition type2)
        {
            DiffSignature(type1, type2);

            if (!AreSameTypeIgnoreNested(type1.BaseType, type2.BaseType))
                throw new InvalidOperationException($"No matching type for {type1.FullName}");

            if (!AreSameTypeIgnoreNested(type1.DeclaringType, type2.DeclaringType))
                throw new InvalidOperationException($"No matching type for {type1.FullName}");

            if (type1.Attributes != type2.Attributes)
                throw new InvalidOperationException($"No matching type for {type1.FullName}");

            Diff(type1.CustomAttributes, type2.CustomAttributes, type1);
        }

        private static void DiffMembers(TypeDefinition type1, TypeDefinition type2, ILogger logger)
        {
            var fields2ByName = type2.Fields.ToLookup(f => f.Name);
            foreach (var field1 in type1.Fields)
            {
                logger.Write($"    field {field1.FullName}");

                var field2 = fields2ByName[field1.Name].SingleOrDefault();
                Diff(field1, field2);

                logger.WriteLineHelp(" OK.");
            }

            var methods2ByName = type2.Methods.ToLookup(f => f.Name);
            var methods2ByComparison = new HashSet<MethodDefinition>(type2.Methods);
            foreach (var method1 in type1.Methods)
            {
                logger.Write($"    method {method1.FullName}");

                var method2 = methods2ByName[method1.Name].SingleOrDefault(m => AreSameSignature(method1, m));
                if (method2 == null)
                    method2 = methods2ByComparison.FirstOrDefault(m => AreSameSignature(method1, m));
                if (method2 != null)
                    methods2ByComparison.Remove(method2);
                else
                    method2 = methods2ByName[method1.Name].SingleOrDefault();

                if (Diff(method1, method2))
                    logger.WriteLineHelp(" OK.");
                else
                    logger.WriteLineInfo(" SKIPPED.");
            }
        }

        private static void Diff(FieldDefinition field1, FieldDefinition field2)
        {
            DiffSignature(field1, field1);

            if (field1.Attributes != field2.Attributes)
                throw new InvalidOperationException($"No matching field for {field1.FullName}");

            Diff(field1.CustomAttributes, field2.CustomAttributes, field1);
        }

        private static bool Diff(MethodDefinition method1, MethodDefinition method2)
        {
            if (IsRunnable(method1.DeclaringType) && IgnoredRunnableMethodNames.Contains(method1.Name))
            {
                return false;
            }

            DiffDefinition(method1, method2);

            DiffVariables(method1, method2);

            DiffBody(method1, method2);

            return true;
        }

        private static void DiffDefinition(MethodDefinition method1, MethodDefinition method2)
        {
            DiffSignature(method1, method2);

            if (method1.Attributes != method2.Attributes)
                throw new InvalidOperationException($"No matching method for {method1}");

            if (method1.ImplAttributes != method2.ImplAttributes)
                throw new InvalidOperationException($"No matching method for {method1}");

            if (method1.Parameters.Count != method2.Parameters.Count)
                throw new InvalidOperationException($"No matching method for {method1}");

            for (int i = 0; i < method1.Parameters.Count; i++)
            {
                var parameter1 = method1.Parameters[i];
                var parameter2 = method2.Parameters[i];
                Diff(parameter1, parameter2);
            }

            Diff(method1.MethodReturnType, method2.MethodReturnType);

            Diff(method1.CustomAttributes, method2.CustomAttributes, method1);
        }

        private static void Diff(ParameterDefinition parameter1, ParameterDefinition parameter2)
        {
            DiffSignature(parameter1, parameter2);

            Diff(parameter1.CustomAttributes, parameter2.CustomAttributes, parameter1);
        }

        private static void Diff(MethodReturnType returnType1, MethodReturnType returnType2)
        {
            if (!AreSameTypeIgnoreNested(returnType1.ReturnType, returnType2.ReturnType))
                throw new InvalidOperationException($"No matching method for {returnType1.Method}");

            if (returnType1.Attributes != returnType2.Attributes)
                throw new InvalidOperationException($"No matching method for {returnType1.Method}");

            Diff(returnType1.CustomAttributes, returnType2.CustomAttributes, returnType1);
        }

        private static void DiffVariables(MethodDefinition method1, MethodDefinition method2)
        {
            var variables1 = method1.Body.Variables.ToList();
            var variables2 = method2.Body.Variables.ToList();
            var diffMax = Math.Min(variables1.Count, variables2.Count);

            for (var i = 0; i < diffMax; i++)
            {
                DiffSignature(variables1[i], variables2[i], method1);
            }

            if (variables1.Count > diffMax)
                throw new InvalidOperationException($"There are additional variables in {method1}.");

            if (variables2.Count > diffMax)
                throw new InvalidOperationException($"There are additional variables in {method2}.");
        }

        private static void DiffBody(MethodDefinition method1, MethodDefinition method2)
        {
            var instructions1 = GetOpInstructions(method1);
            var instructions2 = GetOpInstructions(method2);
            var diffMax = Math.Min(instructions1.Count, instructions2.Count);

            var op2ToOp1Map = instructions1.Take(diffMax)
                .Zip(
                    instructions2.Take(diffMax),
                    (i1, i2) => (i1, i2))
                .ToDictionary(x => x.i2, x => x.i1);

            for (var i = 0; i < diffMax; i++)
            {
                Diff(instructions1[i], instructions2[i], method1, op2ToOp1Map);
            }

            if (instructions1.Count > diffMax)
                throw new InvalidOperationException($"There are additional instructions in {method1}.");

            if (instructions2.Count > diffMax)
                throw new InvalidOperationException($"There are additional instructions in {method2}.");
        }

        private static void Diff(Instruction op1, Instruction op2, MethodDefinition method1, Dictionary<Instruction, Instruction> op2ToOp1Map)
        {
            DiffSignature(op1, op2, method1);

            if (op1.Operand == null)
            {
                // Do nothing
            }
            else if (op1.Operand is TypeReference tr)
            {
                DiffSignature(tr, (TypeReference)op2.Operand);
            }
            else if (op1.Operand is FieldReference fr)
            {
                DiffSignature(fr, (FieldReference)op2.Operand);
            }
            else if (op1.Operand is MethodReference mr)
            {
                DiffSignature(mr, (MethodReference)op2.Operand);
            }
            else if (op1.Operand is ParameterDefinition p)
            {
                DiffSignature(p, (ParameterDefinition)op2.Operand);
            }
            else if (op1.Operand is VariableDefinition v)
            {
                DiffSignature(v, (VariableDefinition)op2.Operand, method1);
            }
            else if (op1.Operand is Instruction i)
            {
                op2ToOp1Map.TryGetValue((Instruction)op2.Operand, out var expectedOp1Operand);
                if (i != expectedOp1Operand)
                    throw new InvalidOperationException($"No matching op for {op1} ({method1}).");
            }
            else if (!Equals(op1.Operand, op2.Operand))
                throw new InvalidOperationException($"No matching op for {op1} ({method1}).");
        }
    }
}