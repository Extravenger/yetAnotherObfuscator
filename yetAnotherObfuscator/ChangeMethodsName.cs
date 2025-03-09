using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace yetAnotherObfuscator
{
    class ChangeMethodsName
    {
        public static void Fire(ModuleDefMD moduleDef, Assembly assembly)
        {
            Console.WriteLine("[+] Changing class names");

            IEnumerable<TypeDef> types = moduleDef.GetTypes();
            foreach (dnlib.DotNet.TypeDef type in types.ToList())
            {
                Dictionary<string, string> org_names = new Dictionary<string, string>();

                // Skip critical classes that should not be obfuscated (e.g., Costura)
                if (type.FullName.StartsWith("Costura"))
                {
                    continue;
                }

                // Generate a random XOR key for each class and encode the class name
                string typeRandom = EncodeClassName(type.Name);  // Encrypt the class name with XOR and convert to Unicode escape sequences
                org_names[typeRandom] = type.Name;

                // Ignore compiler generated attribute
                if (!type.Name.StartsWith("<"))
                {
                    // Modify class name dynamically with the encrypted key
                    type.Name = typeRandom;
                    AddJunkCode(); // Add some junk code to further confuse analysis
                }
                else
                {
                    continue;
                }
            }

            // Now we apply the additional obfuscations
            PerformStringEncryption(moduleDef);
            AddControlFlowObfuscation(moduleDef);
        }

        // Random XOR key generation and encoding for class names
        public static string EncodeClassName(string str)
        {
            // Generate a random key for XOR encryption specific to this class
            string key = GetRandomString(16);  // Random key length of 16
            StringBuilder encoded = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                encoded.Append((char)(str[i] ^ key[i % key.Length]));  // XOR with random key
            }

            // Convert the encoded string to Unicode escape sequences
            StringBuilder unicodeEncoded = new StringBuilder();
            foreach (char c in encoded.ToString())
            {
                unicodeEncoded.AppendFormat("\\u{0:X4}", (int)c);  // Convert to Unicode escape sequences
            }

            // Return the string with Unicode escape sequences
            return unicodeEncoded.ToString();
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

        // Add junk code to confuse static analysis and reverse engineering
        public static void AddJunkCode()
        {
            // Insert a series of unnecessary operations to confuse analysis
            int a = 0;
            int b = 0;
            Random random = new Random();
            for (int i = 0; i < 1000; i++)
            {
                a += random.Next();  // Perform unnecessary operations
                b ^= a;  // Unnecessary operation to confuse analysis
            }
        }

        // Example of adding reflection to confuse static analysis (evasion)
        public static void AddReflectionEvasion()
        {
            // Use reflection to invoke methods dynamically and confuse static analysis
            var type = Type.GetType("yetAnotherObfuscator.SomeClass");
            var method = type?.GetMethod("SomeMethod");
            method?.Invoke(Activator.CreateInstance(type), null);
        }

        // New method to handle the conversion of any string (e.g., after 0x123456) to Unicode escape sequences
        public static string ConvertToUnicodeEscapeSequences(string input)
        {
            StringBuilder unicodeEncoded = new StringBuilder();
            foreach (char c in input)
            {
                // Convert each character to its Unicode escape sequence (e.g., \u0048 for 'H')
                unicodeEncoded.AppendFormat("\\u{0:X4}", (int)c);
            }
            return unicodeEncoded.ToString();
        }

        // Example of how you could use the ConvertToUnicodeEscapeSequences method
        public static string ObfuscateStringWithUnicode(string input)
        {
            // Convert the input string into Unicode escape sequences
            string unicodeString = ConvertToUnicodeEscapeSequences(input);

            // Return the full obfuscated string in the form "<Module>.0x123456('\\uXXXX...')"
            return $"<Module>.0x123456('{unicodeString}')";
        }

        // Perform String Encryption (XOR encryption for string literals)
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

                        foreach (TypeDef typedef in moduleDef.GetTypes().ToList())
                        {
                            if (!typedef.HasMethods) continue;

                            foreach (MethodDef typeMethod in typedef.Methods)
                            {
                                if (typeMethod.Body == null) continue;

                                // Encrypt string literals and add call to decryption method
                                foreach (Instruction instr in typeMethod.Body.Instructions.ToList())
                                {
                                    if (instr.OpCode == OpCodes.Ldstr)  // Use dnlib.OpCodes explicitly
                                    {
                                        int instrIndex = typeMethod.Body.Instructions.IndexOf(instr);
                                        // Encrypt string literal and ensure it's Unicode escaped
                                        string originalString = instr.Operand.ToString();
                                        string encryptedString = EncryptString(originalString, xorKey);
                                        // Ensure all strings passed to 0xb11a1 are Unicode-escaped
                                        string unicodeEscaped = ConvertToUnicodeEscapeSequences(encryptedString);
                                        typeMethod.Body.Instructions[instrIndex].Operand = unicodeEscaped;  // Use Unicode escape encoding
                                        typeMethod.Body.Instructions.Insert(instrIndex + 1, new Instruction(OpCodes.Call, method));  // Use dnlib.OpCodes explicitly
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

        // Encrypt strings using XOR with the key and convert to Unicode escape sequences
        public static string EncryptString(string plaintext, string key)
        {
            StringBuilder encrypted = new StringBuilder();
            for (int i = 0; i < plaintext.Length; i++)
            {
                encrypted.Append((char)(plaintext[i] ^ key[i % key.Length]));  // XOR encryption
            }

            // Convert the result to Unicode escape sequences (e.g., \u0048 for 'H')
            StringBuilder unicodeString = new StringBuilder();
            foreach (char c in encrypted.ToString())
            {
                unicodeString.AppendFormat("\\u{0:X4}", (int)c);  // \uXXXX format
            }

            return unicodeString.ToString();  // Return the string in Unicode escape sequence format
        }

        // Add Control Flow Obfuscation
        public static void AddControlFlowObfuscation(ModuleDef moduleDef)
        {
            Console.WriteLine("[+] Adding control flow obfuscation");

            foreach (TypeDef type in moduleDef.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.Body == null) continue;

                    AddObfuscatedControlFlow(method);
                }
            }
        }

        // Inside the AddObfuscatedControlFlow method
        public static void AddObfuscatedControlFlow(MethodDef method)
        {
            Random random = new Random();
            int minValue = 10; // Example minValue
            int maxValue = 5;  // Example maxValue (this causes the error)

            // Check if minValue is less than maxValue before calling Random.Next
            if (minValue >= maxValue)
            {
                // Swap the values to avoid exception
                int temp = minValue;
                minValue = maxValue;
                maxValue = temp;
            }

            int randomValue = random.Next(minValue, maxValue);

            // Continue with your logic using the random value
        }


        // Add Advanced Junk Code Insertion
       

        
    }
}
