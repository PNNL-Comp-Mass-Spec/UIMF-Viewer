using System;
using System.Collections;
using System.Windows.Forms;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;

namespace IDLTools
{
	interface Persistent
	{
		void LoadContext();
		void SaveContext();
	}

	/// <summary>
	/// Summary description for Persistance.
	/// </summary>
	public class Persistance : MetaData 
	{
		public enum Direction
		{
			GetFromXML,
			SetToXML
		}

		public Persistance()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		//direction is GetFromXML, SetToXML
		public void PersistProperties(Object obj,MetaNode node, Direction dir)
		{
			Type t = obj.GetType();

			foreach(PropertyInfo prop in obj.GetType().GetProperties())
			{
				object[] customAttributes =
					prop.GetCustomAttributes(typeof(Persist),true);
				if(customAttributes.Length > 0)
				{
					try
						{
							//try getting the property from the xml class
							if ( prop.CanWrite && dir==Direction.GetFromXML) 
							{
								Object currentValue = node.GetValue(prop.Name);
								// if there's a value returned,
								if ( currentValue != null )
									// then assign the value to the property
									t.InvokeMember(
										prop.Name,
										BindingFlags.Default | BindingFlags.SetProperty,
										null,
										obj,
										new object [] {currentValue}
										);
							} 
							//try setting the property to xml
							if ( prop.CanRead && dir==Direction.SetToXML) 
							{
								Object currentValue = t.InvokeMember(
									prop.Name,
									BindingFlags.DeclaredOnly | 
									BindingFlags.Public | BindingFlags.NonPublic | 
									BindingFlags.Instance | BindingFlags.GetProperty, null, obj, null);

								node.SetValue (prop.Name, currentValue);
							} 
						}
						catch(Exception e) 
						{
							throw e;
						}
				}
			}
		}

		public void SaveObjectData(Object obj, MetaNode node)
		{
			
		}
		public void LoadObjectData(Object obj, MetaNode node)
		{

		}
		public void ReadFields(Object obj)
		{
			foreach(FieldInfo field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				object[] customAttributes =	field.GetCustomAttributes(typeof(Persist),true);
				if(customAttributes.Length > 0)
				{
					Persist p =(Persist)customAttributes[0];					
					MessageBox.Show (field.Name);
				}
			}
		}


		public void SaveAllObjects(Object [] objs, string file_path)
		{
			MetaData md = new MetaData();
			int i=0;

			foreach (Object obj in objs)
			{				
				if(obj.GetType().BaseType == typeof(Form))	
				{
					MetaNode node = md.OpenChild(obj.GetType().Name);
					SaveContext(node, (Form) obj);
				}
				else
				{
					MetaNode node = md.OpenChild(obj.GetType().Name + i);
					PersistProperties(obj, node, Direction.SetToXML);
					i++;
				}				
			}
			md.WriteFile(file_path);
		}
		/// <description>
		/// Easy saving of forms.
		/// </description>
		public	void	SaveContext(Form frm, string file_path)
		{
			MetaData md = new MetaData();
			MetaNode n = md.OpenChild(frm.Name);
			SaveContext(n, frm);
			md.WriteFile(file_path);
		}

		private void	SaveControlSet(MetaNode node, Control obj)
		{ 
			try 
			{
				foreach (Control cntrl in obj.Controls)
				{
					try
					{
						//only add children that are supported.
						MetaNode child = null;
						if (cntrl.Name == "") 
							child = node;
						else
							child = node.OpenChild(cntrl.Name);
						if (! this.SaveContext(child, cntrl))
							node.RemoveChild(cntrl.Name);
					}
					catch{}
				}
			}
			catch {}
		}

		public	bool	SaveContext(MetaNode node, Form frm)
		{ 			
			node.SetValue ("Left", frm.Left);
			node.SetValue ("Top", frm.Top);
			node.SetValue ("WindowState", frm.WindowState.ToString());
			node.SetValue ("Visible", frm.Visible);
			node.SetValue ("Width", frm.Size.Width);
			node.SetValue ("Height", frm.Size.Height);

			SaveControlSet(node, (Control) frm);

			return(true);
		}

		public	bool	SaveContext(MetaNode node, Control obj)
		{ 			
			MetaNode child = null;

			switch (obj.GetType().Name)
			{
				case "CheckedListBox":
					CheckedListBox cl = (CheckedListBox) obj;
					for (int i = 0; i < cl.Items.Count; i++)
						node.SetValue ("Checked", cl.GetItemCheckState(i), i);
					break;
				case "TextBox": 
					TextBox t = (TextBox) obj;
					node.SetValue ("Text", t.Text);
					break;

				case "NumericUpDown": 
					NumericUpDown n = (NumericUpDown) obj;
					node.SetValue ("Value", n.Value);
					break;

				case "RadioButton": 
					RadioButton r = (RadioButton) obj;
					node.SetValue ("Checked", r.Checked);
					break;

				case "CheckBox": 
					CheckBox c = (CheckBox) obj;
					node.SetValue ("Checked", c.Checked);
					break;

				case "ComboBox": 
					ComboBox cbo = (ComboBox) obj;
					node.SetValue ("SelectedIndex", cbo.SelectedIndex);
					break;

				case "PictureBox": 
					PictureBox pic = (PictureBox) obj;
					child = node.OpenChild ("BackColor");
					child.SetValue ("R", pic.BackColor.R);
					child.SetValue ("G", pic.BackColor.G);
					child.SetValue ("B", pic.BackColor.B);
					break;

				case "ToolBar": 
					ToolBar tb = (ToolBar) obj;
					foreach (ToolBarButton b in tb.Buttons)
                        node.SetValue ("btn", b.Pushed, tb.Buttons.IndexOf(b));
					break;

				case "Panel": 
					Panel p = (Panel) obj;
					child = node.OpenChild ("Size");
					child.SetValue ("Width", p.Width);
					child.SetValue ("Height", p.Height);

					SaveControlSet(node, (Control) p);
					break;

					
				default:
					// not a leaf node. try saving control list, if it has one
					SaveControlSet(node, obj);
					break;
					
			}
			if (node.ChildCount()>0)
				return(true);
			else 
				return(false);
		}

		// Loading
		public	void	LoadContext(Form frm, string file_path)
		{
			MetaData md = new MetaData();
			md.ReadFile(file_path);
			MetaNode n = md.OpenChild(frm.Name);
			LoadContext(n, frm);
		}
		private void	LoadControlSet (MetaNode node, Control obj)
		{ 
			try 
			{
				foreach (Control cntrl in  obj.Controls)
				{
					try
					{

						MetaNode child = null;
						if (cntrl.Name == "")
							child = node;
						else
							child = node.OpenChild(cntrl.Name);
						this.LoadContext(child, cntrl);
					}
					catch{}
				}
			}
			catch {}
		}

		public	void	LoadContext(MetaNode node, Form frm)
		{ 		
			bool visible = true;
			try
			{
				frm.Left = (int)node.GetValue("Left");
				frm.Top = (int)node.GetValue("Top");
				frm.Width = (int)node.GetValue("Width");
				frm.Height = (int)node.GetValue("Height");

				visible = (bool)node.GetValue("Visible");
			
				string state =  (string) node.GetValue("WindowState");

			switch (state)
				{
					case "Maximized":  frm.WindowState = FormWindowState.Maximized; break;
					case "Minimized":  frm.WindowState = FormWindowState.Minimized; break;
					case "Normal":  frm.WindowState = FormWindowState.Normal; break;
				}

				frm.Visible = visible;
			}
			catch{}
			LoadControlSet(node, (Control) frm);
			if (visible)
				frm.Show();
			else
				frm.Hide();
		}

		public	void	LoadContext(MetaNode node, Control obj)
		{ 
			MetaNode child = null;

			try
			{

				switch (obj.GetType().Name)
				{
					case "CheckedListBox":
						CheckedListBox cl = (CheckedListBox) obj;
						for (int i = 0; i < cl.Items.Count; i++)
						{
							cl.SetItemCheckState (i,(CheckState) node.GetValue ("Checked", i));
						}
						break;
					case "TextBox": 
						TextBox t = (TextBox) obj;
						t.Text = (string) node.GetValue ("Text");
						break;

					case "CheckBox": 
						CheckBox c = (CheckBox) obj;
						c.Checked = (bool) node.GetValue("Checked");
						break;

					case "ComboBox": 
						ComboBox cbo = (ComboBox) obj;
						cbo.SelectedIndex = (int) node.GetValue("SelectedIndex");
						break;

					case "NumericUpDown": 
						NumericUpDown n = (NumericUpDown) obj;
						decimal val = (decimal) node.GetValue ("Value");
						if (val>n.Maximum) n.Maximum = val + (decimal) .01;
						if (val<n.Minimum) n.Minimum = val - (decimal) .01;
						n.Value = val;
						break;

					case "RadioButton": 
						RadioButton r = (RadioButton) obj;
						r.Checked = (bool) node.GetValue ("Checked");
						break;

					case "PictureBox": 
						PictureBox pic = (PictureBox) obj;
						child = node.OpenChild ("BackColor");
						int R = (int) child.GetValue ("R");
						int G = (int) child.GetValue ("G");
						int B = (int) child.GetValue ("B");
						pic.BackColor = System.Drawing.Color.FromArgb(R, G, B);
						break;

					case "ToolBar": 
						ToolBar tb = (ToolBar) obj;
						child = node.OpenChild(tb.Name);
						foreach (ToolBarButton b in tb.Buttons)
							b.Pushed = (bool) child.GetValue ("btn",tb.Buttons.IndexOf(b));
						break;

					case "Panel": 
						Panel p = (Panel) obj;
						child = node.OpenChild ("Size");
						p.Width = (int)child.GetValue("Width");
						p.Height = (int)child.GetValue("Height");

						LoadControlSet(node, (Control) p);
						break;

					default:  
						try
						{
							//child = node.OpenChild(obj.Name);
							LoadControlSet(node, obj); 
							//node.RemoveChild(obj.Name);
						}
						catch (Exception e){App.Error(e);}
						break;
				}
			}
			catch{}
		}


		public void		Serialize(Object obj, string file_path)
		{
			XmlSerializer x = new XmlSerializer(obj.GetType());
			TextWriter writer = new StreamWriter(file_path);
			x.Serialize(writer, obj);
			writer.Flush();
			writer.Close();	
		}

		public Object	Deserialize(string file_path, System.Type type)
		{
			if(File.Exists(file_path))
			{
				XmlSerializer x = new XmlSerializer(type);
				TextReader reader = new StreamReader(file_path);
				// Will be returning this.
				Object o = new Object();
				o = x.Deserialize(reader);
				reader.Close();
				return o;
			}
			else
			{
				return null;
			}			
		}

		public void		SerializeBinary(Object obj, string file_path)
		{

		}
		public Object	DeserializeBinary(string file_path, System.Type type)
		{
			return null;
		}
	}
}













