using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Text;
using System.Linq; // For ToList()
using System.Reflection.Emit; // Only if you're using reflection.emit for dynamic type generation
using System.Reflection;

namespace yetAnotherObfuscator
{
    class ManipulateStrings
    {
        static string randomEncryptionKey = GetRandomString(new Random().Next(10, 20));  // Random key length

        public static void PerformStringEncryption(ModuleDef moduleDef)
        {
            string xorKey = GetRandomString(16);  // Generate a random key for encryption

            Console.WriteLine("[+] Injecting the decryption method");

            foreach (TypeDef type in moduleDef.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.Name == "DecryptString")
                    {
                        method.Name = EncryptString("DecryptString", xorKey);  // Encrypt method name
                        method.DeclaringType = null;

                        // Add the method with encrypted name to the module
                        moduleDef.GlobalType.Methods.Add(method);

                        Console.WriteLine("[+] Encrypting all strings with encryption key: " + xorKey);

                        foreach (TypeDef typedef in moduleDef.GetTypes().ToList()) // Use ToList() after adding the using directive
                        {
                            if (!typedef.HasMethods) continue;

                            foreach (MethodDef typeMethod in typedef.Methods)
                            {
                                if (typeMethod.Body == null) continue;

                                // Encrypt string literals and add call to decryption method
                                foreach (Instruction instr in typeMethod.Body.Instructions.ToList())
                                {
                                    if (instr.OpCode == dnlib.DotNet.Emit.OpCodes.Ldstr)  // Use dnlib.OpCodes explicitly
                                    {
                                        int instrIndex = typeMethod.Body.Instructions.IndexOf(instr);
                                        // Encrypt string literal
                                        typeMethod.Body.Instructions[instrIndex].Operand = EncryptString(instr.Operand.ToString(), xorKey);
                                        typeMethod.Body.Instructions.Insert(instrIndex + 1, new Instruction(dnlib.DotNet.Emit.OpCodes.Call, method));  // Use dnlib.OpCodes explicitly
                                    }
                                }

                                typeMethod.Body.UpdateInstructionOffsets();
                                typeMethod.Body.OptimizeBranches();
                                typeMethod.Body.SimplifyBranches();
                            }
                        }
                        break;
                    }
                }
            }
        }

        // Encrypt strings using XOR with the key
        public static string EncryptString(string plaintext, string key)
        {
            StringBuilder encrypted = new StringBuilder();  // <-- StringBuilder is now accessible
            for (int i = 0; i < plaintext.Length; i++)
            {
                encrypted.Append((char)(plaintext[i] ^ key[i % key.Length]));  // XOR encryption
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(encrypted.ToString()));  // Encode to Base64
        }

        // Generate a random string for XOR key
        public static string GetRandomString(int length)
        {
            Random random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        // Add junk code to confuse reverse engineers
        public static void AddJunkCode()
        {
            int a = 0;
            int b = 0;
            for (int i = 0; i < 1000; i++)
            {
                a += i;  // Perform some unnecessary computation
                b += i;  // Perform more unnecessary computation
            }
        }

        // Create dynamic types at runtime using Reflection.Emit
        public static void CreateDynamicType()
        {
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("DynamicAssembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicClass", System.Reflection.TypeAttributes.Public);  // Use System.Reflection.TypeAttributes explicitly

            // Define a method
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("DynamicMethod", System.Reflection.MethodAttributes.Public, typeof(void), null);  // Use MethodAttributes from System.Reflection explicitly
            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.Emit(System.Reflection.Emit.OpCodes.Ret);  // Use System.Reflection.Emit.OpCodes explicitly

            // Create the type
            Type dynamicType = typeBuilder.CreateType();

            // Invoke the method
            object dynamicObject = Activator.CreateInstance(dynamicType);
            dynamicType.GetMethod("DynamicMethod").Invoke(dynamicObject, null);
        }

        // Obfuscate method parameters
        public static void ObfuscateMethodParameters(string param)
        {
            string encodedParam = EncryptString(param, GetRandomString(16));  // Encrypt the parameter
            Console.WriteLine("Obfuscated parameter: " + encodedParam);
            // Decrypt the parameter when needed
            string decryptedParam = DecryptString(encodedParam);
            Console.WriteLine("Decrypted parameter: " + decryptedParam);
        }

        // Decrypt strings
        public static string DecryptString(string ciphertext)
        {
            byte[] decodedData = Convert.FromBase64String(ciphertext);
            string encryptedText = Encoding.UTF8.GetString(decodedData);
            StringBuilder decrypted = new StringBuilder();
            string key = GetRandomString(16);  // Use dynamic key for decryption
            for (int i = 0; i < encryptedText.Length; i++)
            {
                decrypted.Append((char)(encryptedText[i] ^ key[i % key.Length]));  // XOR decryption
            }
            return decrypted.ToString();
        }
    }
}
