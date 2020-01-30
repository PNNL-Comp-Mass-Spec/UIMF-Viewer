#if false
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;

using Microsoft.Win32;

namespace UIMFViewer.Utilities
{
    public enum CalibratorType { A, B, C, D, E };

    public delegate void CalibratorChanged(object sender, EventArgs e);

    public class CalibratorFactory
    {
        static public Calibrator GetCalibrator(CalibratorType type)
        {
            return new CalibratorE();
#if false
            switch(type)
            {
                case CalibratorType.A:
                    return new CalibratorA();
                case CalibratorType.B:
                    return new CalibratorB();
                case CalibratorType.C:
                    return new CalibratorC();
                case CalibratorType.D:
                    return new CalibratorD();
                case CalibratorType.E:
                    return new CalibratorE();
                default:
                    return null;
            }
#endif
        }


        [Editor(typeof(CalibratorEditor), typeof(UITypeEditor))]
        public string Description
        {
            get
            {
                if(_cal != null)
                    return _cal.Description;
                else
                    return "";
            }
            set
            {
                try
                {
                    _cal = GetCalibrator((CalibratorType)Convert.ToInt32(value));
                }
                catch {}
            }
        }


        [Category("Calibration")]
        public Calibrator Cal
        {
            get { return _cal; }
            set
            {
                if(_cal != null)
                    Cal.RegistrySave(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));

                _cal = value;

                if(Changed != null)
                    Changed(this, EventArgs.Empty);
            }
        }

        private Calibrator _cal;

        public event CalibratorChanged Changed;
    }

    internal class CalibratorEditor : UITypeEditor
    {
        private IWindowsFormsEditorService frmsvr;

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            //use IWindowsFormsEditorService object to display a control in the dropdown area
            frmsvr = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if(frmsvr == null)
                return null;

            ListBox lb = new ListBox();

            // Get descriptions for all Calibrator-derived classes
            Array a = Enum.GetValues(typeof(CalibratorType));
            string[] descriptions = new string[a.Length];

            for(int i=0; i<a.Length; i++)
            {
                Calibrator cal = CalibratorFactory.GetCalibrator((CalibratorType)i);
                descriptions[i] = cal.Description;
            }

            lb.Items.AddRange(descriptions);

            // If user double-clicks a number, close the drop down and proceed to return that item.
            lb.DoubleClick += new EventHandler(lb_DoubleClick);

            frmsvr.DropDownControl(lb);
            if(lb.SelectedItem != null)
                return lb.SelectedIndex.ToString();
            else
                return value;
        }


        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        private void lb_DoubleClick(object sender, EventArgs e)
        {
            frmsvr.CloseDropDown();
        }
    }

    /// <summary>
    /// Summary description for Calibrator.
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public abstract class Calibrator : IRegistryPersist
    {
        private float _a;
        private float _b;
        private float _c;

        public Calibrator()
        {
        }

        public abstract float TOFtoMZ(float TOFValue);
        public abstract int MZtoTOF(float mz);
        [Browsable(false)]
        public abstract string Description
        {
            get;
        }


        [Browsable(false)]
        public CalibratorType Type
        {
            get { return _type; }
        }

        public string cal_Description( CalibratorType calType )
        {
            return "Calibrator E";

#if false
            switch (calType)
            {
                case CalibratorType.A:
                    return "Calibrator A";
                case CalibratorType.B:
                    return "Calibrator B";
                case CalibratorType.C:
                    return "Calibrator C";
                case CalibratorType.D:
                    return "Calibrator D";
                case CalibratorType.E:
                    return "Calibrator E";
                default:
                    return null;
            }
#endif
        }

        protected CalibratorType _type;

        public float A
        {
            get { return _a; }
            set { _a = value; }
        }

        public float B
        {
            get { return _b; }
            set { _b = value; }
        }

        public float C
        {
            get { return _c; }
            set { _c = value; }
        }

        #region IRegistryPersist Members

        public abstract void RegistrySave(Microsoft.Win32.RegistryKey key);
        public abstract void RegistryLoad(Microsoft.Win32.RegistryKey key);

        #endregion
    }

    /// <summary>
    /// Summary description for CalibrateA.
    /// mz = (at+b)^2
    /// </summary>
    public class CalibratorA : Calibrator
    {
        public CalibratorA(float a, float b) : this()
        {
            A = a;
            B = b;
            C = (float) 0.0;
        }

        public CalibratorA()
        {
            _type = CalibratorType.A;
            if(A == 0.0f)
                RegistryLoad(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
        }

        public override float TOFtoMZ(float TOFValue)
        {
            // (at+b)^2
            float x = A*TOFValue + B;
            return x*x;
        }

        bool flag_warn_calibration = false;
        public override int MZtoTOF(float mz)
        {
            if (!flag_warn_calibration)
            {
                this.flag_warn_calibration = true;
                MessageBox.Show("Calibration formula A is no longer used, please use Calibration Formula E");
            }

            return 0;

            //return (int) ((Math.Sqrt(mz) - B) / A);
        }

        public override string Description
        {
            get
            {
                return "mz = (at+b)^2";
            }
        }

        public override void RegistrySave(Microsoft.Win32.RegistryKey key)
        {
            using(RegistryKey k = key.CreateSubKey("CalibratorA"))
            {
                k.SetValue("A", A);
                k.SetValue("B", B);
            }
        }
        public override void RegistryLoad(Microsoft.Win32.RegistryKey key)
        {
            try
            {
                using(RegistryKey k = key.CreateSubKey("CalibratorA"))
                {
                    A = Convert.ToSingle(k.GetValue("A"));
                    B = Convert.ToSingle(k.GetValue("B"));
                }
            }
            catch {}
        }
    }

    /// <summary>
    /// Summary description for CalibratorB.
    /// mz = at + b
    /// </summary>
    public class CalibratorB : Calibrator
    {
        public CalibratorB(float a, float b) : this()
        {
            A = a;
            B = b;
            C = (float) 0.0;
        }

        public CalibratorB()
        {
            _type = CalibratorType.B;
            if(A == 0.0f)
                RegistryLoad(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
        }

        public override float TOFtoMZ(float TOFValue)
        {
            return A*TOFValue + B;
        }

        bool flag_warn_calibration = false;
        public override int MZtoTOF(float mz)
        {
            if (!this.flag_warn_calibration)
            {
                this.flag_warn_calibration = true;
                MessageBox.Show("Calibration formula B is no longer used, please use Calibration Formula E");
            }

            return 0;
            //return (int)((mz - B) / A);
        }

        public override string Description
        {
            get
            {
                return "mz = at+b";
            }
        }

        public override void RegistrySave(Microsoft.Win32.RegistryKey key)
        {
            using(RegistryKey k = key.CreateSubKey("CalibratorB"))
            {
                k.SetValue("A", A);
                k.SetValue("B", B);
            }
        }
        public override void RegistryLoad(Microsoft.Win32.RegistryKey key)
        {
            try
            {
                using(RegistryKey k = key.CreateSubKey("CalibratorB"))
                {
                    A = Convert.ToSingle(k.GetValue("A"));
                    B = Convert.ToSingle(k.GetValue("B"));
                }
            }
            catch {}
        }
    }

    /// <summary>
    /// Summary description for CalibrateC.
    /// mz = at^2 + bt + c
    /// </summary>
    public class CalibratorC : Calibrator
    {
        public CalibratorC(float a, float b, float c) : this()
        {
            A = new float();
            A = a;
            B = b;
            C = (float) c;
        }

        public CalibratorC()
        {
            _type = CalibratorType.C;
            if(A == 0.0f)
                RegistryLoad(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
        }

        public override float TOFtoMZ(float TOFValue)
        {
            return (A*TOFValue + B)*TOFValue + C;
        }

        bool flag_warn_calibration = false;
        public override int MZtoTOF(float mz)
        {
            if (!this.flag_warn_calibration)
            {
                this.flag_warn_calibration = true;
                MessageBox.Show("Calibration formula C is no longer used, please use Calibration Formula E");
            }
            return 0;

            /*
            if (A == 0)
            {
                if(B != 0)
                    return (int) ((mz-C) / B);
                else
                    return -1;
            }
            else if(B==0)
            {
                return (int) Math.Sqrt((mz-C)/A);
            }
            else
            {
                // Find roots using quadratic formula
                double c = C-mz;
                double b_2 = B*B;
                double ac4 = 4*A*c;
                if(b_2 - ac4 < 0) // Irrational
                    return -1;

                return (int) ((-B + Math.Sqrt(b_2-ac4)) / (2*A));
            }
            */
        }

        public override string Description
        {
            get
            {
                return "mz = at^2 + bt + c";
            }
        }


        public override void RegistrySave(Microsoft.Win32.RegistryKey key)
        {
            using(RegistryKey k = key.CreateSubKey("CalibratorC"))
            {
                k.SetValue("A", A);
                k.SetValue("B", B);
                k.SetValue("C", C);
            }
        }
        public override void RegistryLoad(Microsoft.Win32.RegistryKey key)
        {
            try
            {
                using(RegistryKey k = key.CreateSubKey("CalibratorC"))
                {
                    A = Convert.ToSingle(k.GetValue("A"));
                    B = Convert.ToSingle(k.GetValue("B"));
                    C = Convert.ToSingle(k.GetValue("C"));
                }
            }
            catch {}
        }

        //private float A, B, C;
    }

    public class CalibratorD : Calibrator
    {
        public CalibratorD(float a, float b) : this()
        {
            A = a;
            B = b;
            C = (float) 0.0;
        }

        public CalibratorD()
        {
            _type = CalibratorType.D;
            if(A == 0.0f)
                RegistryLoad(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
        }

        public override float TOFtoMZ(float TOFValue)
        {
            return A*(TOFValue-B)*(TOFValue-B);
        }

        bool flag_warn_calibration = false;
        public override int MZtoTOF(float mz)
        {
            if (!this.flag_warn_calibration)
            {
                this.flag_warn_calibration = true;
                MessageBox.Show("WARNING:  Calibration formula A is no longer used, please use Calibration Formula E");
            }

            return 0;

            // return (int)(Math.Sqrt(mz / A) + B);
        }

        public override string Description
        {
            get
            {
                return "mz = k(t-t0)^2";
            }
        }


        public override void RegistrySave(Microsoft.Win32.RegistryKey key)
        {
            using (RegistryKey k1 = key.CreateSubKey("CalibratorD"))
            {
                k1.SetValue("k", k);
                k1.SetValue("t0", t0);
            }
        }
        public override void RegistryLoad(Microsoft.Win32.RegistryKey key)
        {
            try
            {
                using(RegistryKey k1 = key.CreateSubKey("CalibratorD"))
                {
                    k = Convert.ToSingle(k1.GetValue("k"));
                    t0 = Convert.ToSingle(k1.GetValue("t0"));
                }
            }
            catch {}
        }

        public float k
        {
            get { return A; }
            set { A = value; }
        }

        public float t0
        {
            get { return B; }
            set { B = value; }
        }

        //private float A, B;
    }

    /// <summary>
    /// Calibrate TOF to m/z according to formula mass = (k * (t-t0))^2
    /// </summary>
    public class CalibratorE : Calibrator
    {
        public CalibratorE(float a, float b) : this()
        {
            A = a;
            B = b;
            C = (float) 0.0;
        }

        public CalibratorE()
        {
            _type = CalibratorType.E;
            if(A == 0.0f)
                RegistryLoad(Registry.CurrentUser.CreateSubKey("Software").CreateSubKey(AppDomain.CurrentDomain.FriendlyName));
        }

        public override float TOFtoMZ(float TOFValue)
        {
            float r = A*(TOFValue-B);
            return r*r;
        }

        public override int MZtoTOF(float mz)
        {
            double r = (Math.Sqrt(mz));
            return (int)(((r/A)+B)+.5); // .5 for rounding
        }

        public override string Description
        {
            get
            {
                return "mz = (k*(t-t0))^2";
            }
        }


        public override void RegistrySave(Microsoft.Win32.RegistryKey key)
        {
            using(RegistryKey k1 = key.CreateSubKey("CalibratorD"))
            {
                k1.SetValue("k", k);
                k1.SetValue("t0", t0);
            }
        }
        public override void RegistryLoad(Microsoft.Win32.RegistryKey key)
        {
            try
            {
                using(RegistryKey k1 = key.CreateSubKey("CalibratorD"))
                {
                    k = Convert.ToSingle(k1.GetValue("k"));
                    t0 = Convert.ToSingle(k1.GetValue("t0"));
                }
            }
            catch {}
        }

        public float k
        {
            get { return A; }
            set { A = value; }
        }

        public float t0
        {
            get { return B; }
            set { B = value; }
        }

        //private float A, B;
    }
}
#endif
