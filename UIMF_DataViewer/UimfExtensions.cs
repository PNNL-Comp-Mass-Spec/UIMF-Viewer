using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIMFLibrary;

namespace UIMF_File
{
    public static class UimfExtensions
    {
        /// <summary>
        /// Returns a deep-copy of this object
        /// </summary>
        /// <returns></returns>
        public static GlobalParams Clone(this GlobalParams orig)
        {
            var newParams = new GlobalParams();
            foreach (var value in orig.Values)
            {
                newParams.Values.Add(value.Key, new GlobalParam(value.Value.ParamType, value.Value.Value));
            }

            return newParams;
        }

        /// <summary>
        /// Returns a deep-copy of this object
        /// </summary>
        /// <returns></returns>
        public static FrameParams Clone(this FrameParams orig)
        {
            var newParams = new FrameParams();
            foreach (var value in orig.Values)
            {
                newParams.Values.Add(value.Key, new FrameParam(value.Value.Definition, value.Value.Value));
            }

            return newParams;
        }
    }
}
