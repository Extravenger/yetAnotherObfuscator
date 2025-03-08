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
    }
}
