using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Text;
using System.Linq;

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

                        foreach (TypeDef typedef in moduleDef.GetTypes().ToList())
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
                                        // Encrypt string literal and ensure it's Unicode escaped
                                        string originalString = instr.Operand.ToString();
                                        string encryptedString = EncryptString(originalString, xorKey);
                                        // Ensure all strings passed to 0xb11a1 are Unicode-escaped
                                        string unicodeEscaped = ConvertToUnicodeEscapeSequences(encryptedString);
                                        typeMethod.Body.Instructions[instrIndex].Operand = unicodeEscaped;  // Use Unicode escape encoding
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

        // Convert a string to Unicode escape sequences
        public static string ConvertToUnicodeEscapeSequences(string input)
        {
            StringBuilder unicodeEncoded = new StringBuilder();
            foreach (char c in input)
            {
                unicodeEncoded.AppendFormat("\\u{0:X4}", (int)c);
            }
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

        // Decrypt strings
        public static string DecryptString(string ciphertext)
        {
            // Convert Unicode escape sequence (e.g., \u0048) back to characters
            StringBuilder decrypted = new StringBuilder();
            for (int i = 0; i < ciphertext.Length; i++)
            {
                if (ciphertext[i] == '\\' && i + 5 < ciphertext.Length && ciphertext[i + 1] == 'u')
                {
                    // Extract the Unicode escape sequence
                    string hexValue = ciphertext.Substring(i + 2, 4);
                    int charCode = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
                    decrypted.Append((char)charCode);
                    i += 5;  // Skip over the \uXXXX sequence
                }
                else
                {
                    decrypted.Append(ciphertext[i]);
                }
            }

            // Now decrypt with XOR (the same way it was encrypted)
            string key = GetRandomString(16);  // Use a dynamic key for decryption
            StringBuilder finalDecrypted = new StringBuilder();
            for (int i = 0; i < decrypted.Length; i++)
            {
                finalDecrypted.Append((char)(decrypted[i] ^ key[i % key.Length]));  // XOR decryption
            }

            return finalDecrypted.ToString();
        }
    }
}
