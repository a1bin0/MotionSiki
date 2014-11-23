using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNI;

namespace Siki
{
    class MotionDetector
    {
        #region

        private DepthGenerator depth;
        
        #endregion

        private Dictionary<int, List<Dictionary<SkeletonJoint, SkeletonJointPosition>>> histryData = new Dictionary<int,List<Dictionary<SkeletonJoint,SkeletonJointPosition>>>();

        public event EventHandler LeftHandUpDetected;
        public event EventHandler LeftHandDownDetected;
        public event EventHandler LeftHandSwipeLeftDetected;
        public event EventHandler LeftHandOverHeadDetected;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MotionDetector (DepthGenerator depth)
        {
            this.depth = depth;
        }

        public void DetectMotion(int userID, Dictionary<SkeletonJoint, SkeletonJointPosition> skeleton)
        {
            saveHistory(userID, skeleton);

            if (DetectLeftHandSwipeUp(userID, skeleton))
            {
                this.LeftHandUpDetected(this, EventArgs.Empty);
            }

            if (DetectLeftHandDown(userID, skeleton))
            {
                this.LeftHandDownDetected(this, EventArgs.Empty);
            }

            if (DetectLeftHandSwipeLeft(userID, skeleton))
            {
                this.LeftHandSwipeLeftDetected(this, EventArgs.Empty);
            }

            if (DetectLeftHandOverHead(userID, skeleton))
            {
                this.LeftHandOverHeadDetected(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private void saveHistory(int userID, Dictionary<SkeletonJoint, SkeletonJointPosition> skeleton)
        {
            if (!this.histryData.ContainsKey(userID))
            {
                this.histryData.Add(userID, new List<Dictionary<SkeletonJoint, SkeletonJointPosition>>());
            }

            List<Dictionary<SkeletonJoint, SkeletonJointPosition>> positions = this.histryData[userID];

            if (positions.Count > 4)
            {
                positions.RemoveAt(0);
            }

            positions.Add(skeleton);
        }

        /// <summary>
        /// 左手が頭より横に30cm以上離れておらず、頭の上にあるか,
        /// 　TODO両肩の座標とって比較しよう
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private bool DetectLeftHandOverHead(int userID, Dictionary<SkeletonJoint, SkeletonJointPosition> skeleton)
        {
            float distance = Math.Abs
                ((depth.ConvertRealWorldToProjective(skeleton[SkeletonJoint.LeftHand].Position).X
                - depth.ConvertRealWorldToProjective(skeleton[SkeletonJoint.Head].Position).X));

            if (distance > 30.0) return false;

            return (depth.ConvertRealWorldToProjective(skeleton[SkeletonJoint.LeftHand].Position).Y
                < depth.ConvertRealWorldToProjective(skeleton[SkeletonJoint.Head].Position).Y);
        }

        /// <summary>
        /// 左手が頭から約30cm以上離れた場所で左に移動したか
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private bool DetectLeftHandSwipeLeft(int userID, Dictionary<SkeletonJoint, SkeletonJointPosition> skeleton)
        {
            List<Dictionary<SkeletonJoint, SkeletonJointPosition>> positions = this.histryData[userID];

            if (positions.Count < 5) return false;

            for (int i = 0; i < 4; i++)
            {
                Point3D oldData = depth.ConvertRealWorldToProjective(positions[i][SkeletonJoint.LeftHand].Position);
                Point3D newData = depth.ConvertRealWorldToProjective(positions[i + 1][SkeletonJoint.LeftHand].Position);
                if (oldData.X < newData.X) return false;
            }

            return positions.All(item => this.isFar(item));
        }

        /// <summary>
        /// 左手が頭から約30cm以上離れた場所で下がっているか
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private bool DetectLeftHandDown(int userID, Dictionary<SkeletonJoint, SkeletonJointPosition> skeleton)
        {
            List<Dictionary<SkeletonJoint, SkeletonJointPosition>> positions = this.histryData[userID];

            if (positions.Count < 5) return false;

            for (int i = 0; i < 4; i++)
            {
                Point3D oldData = depth.ConvertRealWorldToProjective(positions[i][SkeletonJoint.LeftHand].Position);
                Point3D newData = depth.ConvertRealWorldToProjective(positions[i + 1][SkeletonJoint.LeftHand].Position);
                if (oldData.Y > newData.Y) return false;
            }

            return positions.All(item => this.isFar(item));
        }

        /// <summary>
        /// 左手が頭から約30cm以上離れた場所で上がっているか
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private bool DetectLeftHandSwipeUp(int userID, Dictionary<SkeletonJoint, SkeletonJointPosition> skeleton)
        {
            List<Dictionary<SkeletonJoint, SkeletonJointPosition>> positions = this.histryData[userID];

            if (positions.Count < 5) return false;

            for (int i = 0; i < 4; i++)
            {
                Point3D oldData = depth.ConvertRealWorldToProjective(positions[i][SkeletonJoint.LeftHand].Position);
                Point3D newData = depth.ConvertRealWorldToProjective(positions[i + 1][SkeletonJoint.LeftHand].Position);
                if (oldData.Y < newData.Y) return false;
            }

            return positions.All(item => this.isFar(item));
        }

        /// <summary>
        /// 頭と左手が約30cm以上横に離れているか
        /// </summary>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private bool isFar(Dictionary<SkeletonJoint, SkeletonJointPosition> skeleton)
        {
            float distance = 
                (depth.ConvertRealWorldToProjective(skeleton[SkeletonJoint.Head].Position).X
                - depth.ConvertRealWorldToProjective(skeleton[SkeletonJoint.LeftHand].Position).X);

            return (distance > 30.0);
        }
    }
}
