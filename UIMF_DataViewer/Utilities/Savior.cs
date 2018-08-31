using System;
// =====================================================================
//
// Savior - Simplify Saving and Restoring Application Settings
//
// by Jim Hollenhorst, jim@ultrapico.com
// Copyright Ultrapico, March 2003
// http://www.ultrapico.com
//
// =====================================================================
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace SaviorClass
{
    /// This namespace is used for saving data to the registry. A very simple class
    /// is customized for each application by storing all the variables
    /// that are to be saved as public fields, with default values specified.
    /// The work is done by the "Savior" class. It has static methods to
    /// Save and Read the data from the registry or a file.
    ///
    /// The following data types are supported:
    ///
    /// string, bool, decimal, int, float, double, Color, Point, Size, Font,
    /// DateTime, TimeSpan, int[], byte[], string[], bool[], float[], double[],
    /// as well as any enum class whose definition is visible to the class.
    ///

    /// The method calls are all static and given as follows:
    ///
  /// void Save(Settings)                  --> Saves the settings to the default registry key
  /// void Save(Settings,string)           --> Saves the settings to a specified key name in HKCU
  /// void Save(Settings,RegistryKey)      --> Saves the settings to a specified registry key
  /// void SaveToFile(Settings,string)     --> Saves the settings to a specified file
  ///
    /// void Read(Settings)                  --> Reads the settings from the default registry key
    /// void Read(Settings,string)           --> Reads the settings from a specified key name in HKCU
    /// void Read(Settings,RegistryKey)      --> Reads the settings from a specified registry key
    /// object ReadFromFile(string)          --> Reads the settings from a specified file
    ///
    /// string ToString()                    --> Returns information about the settings
    ///
    /// Note that the ReadFromFile method works differently from the Read methods. It returns the
    /// settings as on object, which must then be cast to the proper type.
    ///
    /// A proper call of this method would look like this:
    ///
    /// settings = (Settings)Savior.ReadFromFile(FileName);
    ///

    /// <summary>
    /// Savior is the class that is used to save and restore data using the registry
    /// or by binary serialization in a file. It works in conjuction with another class
    /// (with the recommended name "Settings") that contains all the data to be
    /// stored.
    /// </summary>
    public class Savior
    {

        /// <summary>
        /// Returns a list of all the fields and field types for the specified class
        /// Useful for debugging.
        /// </summary>
        public static string ToString(object settings)
        {
            string msg="";
            foreach(FieldInfo fi in settings.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                string TheValue="";
                if(fi.GetValue(settings)!=null)TheValue=fi.GetValue(settings).ToString();
                msg+="Name: "+fi.Name+" = "+TheValue+
                    "\n    FieldType: "+fi.FieldType+"\n\n";
            }
            return msg;
        }

        /// <summary>
        /// Save all the information in a class to the registry. This method iterates through
        /// all members of the specified class and saves them to the registry.
        /// </summary>
        /// <param name="settings">An object that holds all the desired settings</param>
        /// <param name="Key">The registry key in which to save data</param>
        public static void Save(object settings, RegistryKey Key)
        {
            RegistryKey subkey;

            // Iterate through each Field of the specified class using "Reflection".
            // The name of each Field is used to generate the name of the registry
            // value, sometimes with appended characters to specify elements of more
            // complex Field types such as a Font or a Point. Arrays are stored by
            // creating a new subkey with the same name as the Field. The subkey holds
            // values with names that are just the integers 0,1,2,... giving the index
            // of each value.
            //

            foreach(FieldInfo fi in settings.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                // If this field is an Enum it needs to be handled separately. The text
                // name for the enum is written to the registry.
                if(fi.FieldType.IsEnum)
                {
                    Key.SetValue(fi.Name,Enum.GetName(fi.FieldType,fi.GetValue(settings)));
                }

                // Otherwise each different field type is handled case by case using
                // a large switch statement
                else
                {
                    //MessageBox.Show(fi.FieldType.Name.ToLower());

                    // Test the name of the Field type, converted to lower case
                    switch (fi.FieldType.Name.ToLower())
                    {
                        case "string":
                            Key.SetValue(fi.Name,(string)fi.GetValue(settings));
                            break;

                        case "boolean":
                            Key.SetValue(fi.Name,(bool)fi.GetValue(settings));
                            break;

                        case "int32":
                            Key.SetValue(fi.Name,(int)fi.GetValue(settings));
                            break;

                        case "decimal":
                            decimal TheDecimal=(decimal)fi.GetValue(settings);
                            Key.SetValue(fi.Name,(int)TheDecimal);
                            break;

                        case "single":
                            Key.SetValue(fi.Name,(float)fi.GetValue(settings));
                            break;

                        case "double":
                            Key.SetValue(fi.Name,(double)fi.GetValue(settings));
                            break;

                        // Store a Point as two separate integers
                        case "point":
                            Point point=(Point)fi.GetValue(settings);
                            Key.SetValue(fi.Name+".X",point.X);
                            Key.SetValue(fi.Name+".Y",point.Y);
                            break;

                        // Store a Size as two separate integers
                        case "size":
                            Size size=(Size)fi.GetValue(settings);
                            Key.SetValue(fi.Name+".Height",size.Height);
                            Key.SetValue(fi.Name+".Width",size.Width);
                            break;

                        // byte arrays are a native registry type and thus easy to handle
                        case "byte[]":
                            byte[] bytes = (byte[])fi.GetValue(settings);
                            if(bytes==null)break;
                            Key.SetValue(fi.Name,bytes);
                            break;

                        // string arrays are a native registry type and thus easy to handle
                        case "string[]":
                            string[] strings = (string[])fi.GetValue(settings);
                            if(strings==null)break;
                            Key.SetValue(fi.Name,strings);
                            break;

                        // For arrays that aren't native registry types, create a subkey and then
                        // create values that are numbered sequentially. First delete the subkey
                        // if it already exists then refill it with all the values of the array.
                        case "int32[]":
                            int[] numbers = (int[])fi.GetValue(settings);
                            if(numbers==null)break;
                            Key.DeleteSubKey(fi.Name,false);   // first delete all the old values
                            subkey=Key.CreateSubKey(fi.Name);
                            for(int i=0;i<numbers.Length;i++)
                            {
                                subkey.SetValue(i.ToString(),numbers[i]);
                            }
                            break;

                        case "boolean[]":
                            bool[] bools = (bool[])fi.GetValue(settings);
                            if(bools==null)break;
                            Key.DeleteSubKey(fi.Name,false);   // first delete all the old values
                            subkey=Key.CreateSubKey(fi.Name);
                            for(int i=0;i<bools.Length;i++)
                            {
                                subkey.SetValue(i.ToString(),bools[i]);
                            }
                            break;

                        case "single[]":
                            float[] floats = (float[])fi.GetValue(settings);
                            if(floats==null)break;
                            Key.DeleteSubKey(fi.Name,false);   // first delete all the old values
                            subkey=Key.CreateSubKey(fi.Name);
                            for(int i=0;i<floats.Length;i++)
                            {
                                subkey.SetValue(i.ToString(),floats[i]);
                            }
                            break;

                        case "double[]":
                            double[] doubles = (double[])fi.GetValue(settings);
                            if(doubles==null)break;
                            Key.DeleteSubKey(fi.Name,false);   // first delete all the old values
                            subkey=Key.CreateSubKey(fi.Name);
                            for(int i=0;i<doubles.Length;i++)
                            {
                                subkey.SetValue(i.ToString(),doubles[i]);
                            }
                            break;

                        // Saving colors is easy, unlike reading them, which is trickier. We just use the Color's
                        // Name property. If there is no known name, the Argb value will be written in hexadecimal.
                        case "color":
                            Key.SetValue(fi.Name,((Color)fi.GetValue(settings)).Name);
                            break;

                        // Save a TimeSpan using it's ToString() method
                        case "timespan":
                            Key.SetValue(fi.Name,((TimeSpan)fi.GetValue(settings)).ToString());
                            break;

                        // Save a DateTime using it's ToString() method
                        case "datetime":
                            Key.SetValue(fi.Name,((DateTime)fi.GetValue(settings)).ToString());
                            break;

                        // Save a Font by separately storing Name, Size, and Style
                        case "font":
                            Key.SetValue(fi.Name+".Name",((Font)fi.GetValue(settings)).Name);
                            Key.SetValue(fi.Name+".Size",((Font)fi.GetValue(settings)).Size);
                            Key.SetValue(fi.Name+".Style",((Font)fi.GetValue(settings)).Style);
                            break;

                        default:
                            MessageBox.Show("This Field type cannot be saved by the Savior class: "+fi.FieldType);
                            break;
                    }
                }
            }
        }

        // Here are several overloads for the Save routine, specifying the RegistryKey in
        // several different ways

        /// <summary>
        /// Save to the registry using the specified key
        /// </summary>
        /// <param name="key">A string giving the registry key path relative to HKCU</param>
        public static void Save(object settings, string key)
        {
            RegistryKey Key = Registry.CurrentUser.CreateSubKey(key);
            Save(settings,Key);
        }

        /// <summary>
        /// Save to the registry using the default key, the standard user application
        /// data registry key. To use this effectively, be sure to specify the
        /// appropriate information in the AssemblyInfo file.
        /// </summary>
        public static void Save(object settings)
        {
            RegistryKey Key=Application.UserAppDataRegistry;
            Save(settings,Key);
        }


        /// <summary>
        /// Read all the information in a class to the registry. This method iterates through
        /// all members of the specified class and reads them to the registry.
        /// </summary>
        /// <param name="settings">An object that holds all the desired settings</param>
        /// <param name="Key">The registry key in which to save data</param>
        public static void Read(object settings, RegistryKey Key)
        {
            // Iterate through each Field of the specified class using "Reflection".
            // The name of each Field is used to generate the name of the registry
            // value, sometimes with appended characters to specify elements of more
            // complex Field types such as a Font or a Point. Arrays are read from
            // a subkey with the same name as the Field. The subkey holds
            // values with names that are just the integers 0,1,2,... giving the index
            // of each value.
            //
            foreach(FieldInfo fi in settings.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                object obj;
                int X,Y,Height,Width;
                string name;
                float emSize;
                FontStyle style;
                RegistryKey subkey;

                // If this field is an Enum it needs to be handled separately. The text
                // name for the enum is read from the registry.
                if(fi.FieldType.IsEnum)
                {
                    if((obj=Key.GetValue(fi.Name))!=null)
                        fi.SetValue(settings,Enum.Parse(fi.FieldType,(string)obj));
                }

                // Otherwise each different field type is handled case by case using
                // a large switch statement that tests the lower case name of the Field type
                else
                {
                    switch (fi.FieldType.Name.ToLower())
                    {
                        case "string":
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,(string)obj);
                            break;

                        case "boolean":
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,bool.Parse((string)obj));
                            break;

                        case "int32":
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,(int)obj);
                            break;

                        case "decimal":
                            if((obj=Key.GetValue(fi.Name))!=null)
                            {
                                int TheInt=(int)obj;
                                fi.SetValue(settings,(decimal)TheInt);
                            }
                            break;

                        case "single":
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,float.Parse((string)obj));
                            break;

                        case "double":
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,double.Parse((string)obj));
                            break;

                        case "point":
                            if((obj=Key.GetValue(fi.Name+".X"))!=null)
                            {
                                X=(int)obj;
                                if((obj=Key.GetValue(fi.Name+".Y"))!=null)
                                {
                                    Y=(int)obj;
                                    fi.SetValue(settings,new Point(X,Y));
                                    //MessageBox.Show("X:"+point.X+" Y:"+point.Y);
                                }
                            }
                            break;

                        case "size":
                            if((obj=Key.GetValue(fi.Name+".Height"))!=null)
                            {
                                Height=(int)obj;
                                if((obj=Key.GetValue(fi.Name+".Width"))!=null)
                                {
                                    Width=(int)obj;
                                    fi.SetValue(settings,new Size(Width,Height));
                                }
                            }
                            break;

                        // string arrays are a native registry type and thus easy to handle
                        case "string[]":  // Get an array of strings
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,(string[])obj);
                            break;

                        // byte arrays are a native registry type and thus, easy to handle
                        case "byte[]":  // Get an array of bytes
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,(byte[])obj);
                            break;

                        case "int32[]":  // Get an array of ints
                            if((subkey=Key.OpenSubKey(fi.Name))!=null)
                            {
                                int i=0;
                                int N=subkey.ValueCount;
                                int[] integers = new int[N];
                                while((obj=subkey.GetValue(i.ToString()))!=null)
                                    integers[i++]=(int)obj;
                                fi.SetValue(settings,integers);
                            }
                            break;

                        case "single[]":  // Get an array of floats
                            if((subkey=Key.OpenSubKey(fi.Name))!=null)
                            {
                                int i=0;
                                int N=subkey.ValueCount;
                                float[] floats = new float[N];
                                while((obj=subkey.GetValue(i.ToString()))!=null)
                                {
                                    floats[i++]=float.Parse((string)obj);
                                }
                                fi.SetValue(settings,floats);
                            }
                            break;

                        case "double[]":  // Get an array of doubles
                            if((subkey=Key.OpenSubKey(fi.Name))!=null)
                            {
                                int i=0;
                                int N=subkey.ValueCount;
                                double[] doubles = new double[N];
                                while((obj=subkey.GetValue(i.ToString()))!=null)
                                {
                                    doubles[i++]=double.Parse((string)obj);
                                }
                                fi.SetValue(settings,doubles);
                            }
                            break;

                        case "boolean[]":  // Get an array of booleans
                            if((subkey=Key.OpenSubKey(fi.Name))!=null)
                            {
                                int i=0;
                                int N=subkey.ValueCount;
                                bool[] bools = new bool[N];
                                while((obj=subkey.GetValue(i.ToString()))!=null)
                                {
                                    bools[i]=bool.Parse((string)obj);
                                    i++;
                                }
                                fi.SetValue(settings,bools);
                            }
                            break;

                        // Colors are tricky. If it is a known named color it is easy, we just
                        // use Color.FromName. If it is not a named color, than we have to decode
                        // the Argb values from the hexadecimal number.
                        // So we check to see if the string is a hexadecimal number and, if so
                        // decode it as an Argb value, otherwise we reconstruct from the name
                        // If the name is invalid, we should just get a default color value
                        case "color":  // Get a Color
                            if((obj=Key.GetValue(fi.Name))!=null)
                            {
                                string TheColorName = (string)obj;
                                Color TheColor;
                                if(IsHexadecimal(TheColorName))
                                    TheColor = Color.FromArgb(Int32.Parse(TheColorName,NumberStyles.HexNumber));
                                else
                                    TheColor = Color.FromName(TheColorName);
                                fi.SetValue(settings,TheColor);
                            }
                            break;

                        case "timespan":  // Get a TimeSpan
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,TimeSpan.Parse((string)obj));
                            break;

                        case "datetime":  // Get a DateTime
                            if((obj=Key.GetValue(fi.Name))!=null)
                                fi.SetValue(settings,DateTime.Parse((string)obj));
                            break;

                        case "font":
                            if((obj=Key.GetValue(fi.Name+".Name"))!=null)
                            {
                                name=(string)obj;
                                if((obj=Key.GetValue(fi.Name+".Size"))!=null)
                                {
                                    emSize=float.Parse((string)obj);
                                    if((obj=Key.GetValue(fi.Name+".Style"))!=null)
                                    {
                                        style=(FontStyle)Enum.Parse(typeof(FontStyle),(string)obj);
                                        fi.SetValue(settings,new Font(name,emSize,style));
                                    }
                                }
                            }
                            break;

                        default:
                            MessageBox.Show("This type has not been implemented: "+fi.FieldType);
                            break;
                    }
                }
            }
        }

        // Here are several overloads for the Read routine, specifying the RegistryKey in
        // several different ways

        /// <summary>
        /// Read from the registry using the specified key
        /// </summary>
        /// <param name="key">A string giving the registry key path relative to HKCU</param>
        public static void Read(object settings, string key)
        {
            RegistryKey Key = Registry.CurrentUser.CreateSubKey(key);
            Read(settings,Key);
        }

        /// <summary>
        /// Read from the registry using the default key, the standard user application
        /// data registry key. To use this effectively, be sure to specify the
        /// appropriate information in the AssemblyInfo file.
        /// </summary>
        public static void Read(object settings)
        {
            RegistryKey Key=Application.UserAppDataRegistry;
            Read(settings,Key);
        }


        /// <summary>
        /// Save settings to a file using binary serialization
        /// </summary>
        /// <param name="settings">This is the object that we want to serialize</param>
        /// <param name="FileName">The name of the file in which to store settings</param>
        public static bool SaveToFile(object settings, string FileName)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(FileName, FileMode.Create,
                    FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, settings);
                stream.Close();
                return true;
            }
            catch
            {
                MessageBox.Show("Error attempting to save the settings to a file\n\n"+FileName,
                    "Expresso Error",
                    MessageBoxButtons.OK,MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Save settings to a file using XML serialization
        /// </summary>
        /// <param name="settings">This is the object that we want to serialize</param>
        /// <param name="FileName">The name of the file in which to store settings</param>
        public static bool SaveToFileXML(object o, string FileName)
        {
            TextWriter tw = null;
            XmlWriter writer = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(o.GetType());

                // A FileStream is needed to read the XML document.
                tw = new StreamWriter(FileName);
                writer = new XmlTextWriter(tw);

                serializer.Serialize(writer, o);

                writer.Close();
                tw.Close();
                return true;
            }
            catch
            {
                if(writer != null)
                    writer.Close();
                if(tw != null)
                    tw.Close();

                MessageBox.Show("Error attempting to save the settings to a file\n\n"+FileName,
                    "Expresso Error",
                    MessageBoxButtons.OK,MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Read from a file using binary serialization. Normally a call to this method would
        /// cast the return value to the correct type as in this example:
        ///
        /// settings = (Settings)Savior.ReadFromFile(FileName);
        ///
        /// </summary>
        /// <param name="FileName">The name of the file from which to read settings</param>
        /// <returns>An object is returned containing the settings</returns>
        public static object ReadFromFile(string FileName)
        {
            try
            {
                // First try to read the version information
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(FileName, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                object NewSettings = (object)formatter.Deserialize(stream);
                stream.Close();
                return NewSettings;
            }
            catch
            {
                MessageBox.Show("Error attempting to read the settings from a file\n\n"+FileName,
                    "Expresso Error", MessageBoxButtons.OK,MessageBoxIcon.Error);
                return null;   // If there is an error return null
            }
        }

        /// <summary>
        /// Read from a file using XML serialization. Normally a call to this method would
        /// cast the return value to the correct type as in this example:
        ///
        /// settings = (Settings)Savior.ReadFromFile(FileName);
        ///
        /// </summary>
        /// <param name="FileName">The name of the file from which to read settings</param>
        /// <returns>An object is returned containing the settings</returns>
        public static object ReadFromFileXML(string FileName, Type type)
        {
           // MessageBox.Show("ReadFromFileXML:  "+FileName);
            FileStream fs = null;
            XmlReader reader = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(type);

                // A FileStream is needed to read the XML document.
                fs = new FileStream(FileName, FileMode.Open);
                reader = new XmlTextReader(fs);

                object o = serializer.Deserialize(reader);

                reader.Close();
                fs.Close();

                // Use the Deserialize method to restore the object's state.
                return o;
            }
            catch(Exception ex)
            {
                if(reader != null)
                    reader.Close();
                if(fs != null)
                    fs.Close();

                MessageBox.Show(ex.ToString() + "\nError attempting to read the settings from a file\n\n"+FileName,
                    "Expresso Error",
                    MessageBoxButtons.OK,MessageBoxIcon.Error);
                return null;   // If there is an error return null
            }
        }

        /// <summary>
        /// This returns true if the input is a string of valid hexadecimal digits 0-9, A-F, or a-f
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>true if the input is a valid hex number</returns>
        public static bool IsHexadecimal(string input)
        {
            foreach ( char c in input)
            {
                if(!Uri.IsHexDigit(c))return false;
            }
            return true;
        }
    }
}
