using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HeadDirectionWpf
{

    class OpenposeJsonSequence
    {
        public List<OpenposeOutput> OpenposeOutputs { get; set; }
    }

    class OpenposeOutput
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("people")]
        public List<People> Peoples { get; set; }
    }

    class People
    {
        public float[] Pose_keypoints_2d { get; set; }
    }
}
