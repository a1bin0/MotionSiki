using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using OpenNI;
using System.Collections.Generic;

namespace Siki
{
    //region書いて！

    public partial class Siki : Form
    {
        private Context context;
        private ScriptNode scriptNode;
        private Thread readerThread;
        private bool shouldRun;
        private DepthGenerator depth;
        private UserGenerator user;
        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private MotionDetector motionDetector;

        public Siki()
        {
            InitializeComponent();

            this.context = Context.CreateFromXmlFile(@"./Config.xml", out this.scriptNode);
            this.context.GlobalMirror = true;
            this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            this.user = new UserGenerator(context);

            setupMotiondetector();

            user.NewUser += this.user_NewUser;
            user.LostUser += this.user_Lost;
            user.SkeletonCapability.CalibrationStart += this.SkeletonCapability_CalibrationStart;
            user.SkeletonCapability.CalibrationComplete += this.SkeletonCapability_CalibrationComplete;
            user.SkeletonCapability.SetSkeletonProfile(SkeletonProfile.All);
            context.StartGeneratingAll();

            this.shouldRun = true;
            readerThread = new Thread(new ThreadStart(ReaderThread));
            this.readerThread.Start();
        }

        private void setupMotiondetector()
        {
            this.motionDetector = new MotionDetector(this.depth);

            this.motionDetector.LeftHandDownDetected += this.leftHandDownDetected;
            this.motionDetector.LeftHandUpDetected += this.leftHandUpDetected;
            this.motionDetector.LeftHandSwipeLeftDetected += this.leftHandSwipeLeftDetected;
            this.motionDetector.LeftHandOverHeadDetected += this.leftHandOverHeadDetected;
        }

        private void leftHandDownDetected(object sender, EventArgs e)
        {
            Console.WriteLine("down");
        }

        private void leftHandUpDetected(object sender, EventArgs e)
        {
            Console.WriteLine("up");
        }

        private void leftHandSwipeLeftDetected(object sender, EventArgs e)
        {
            Console.WriteLine("swipeLeft");
        }

        private void leftHandOverHeadDetected(object sender, EventArgs e)
        {
            Console.WriteLine("overHead");
        }

        void SkeletonCapability_CalibrationComplete(object sender, CalibrationProgressEventArgs e)
        {
            Console.WriteLine(String.Format("キャリブレーション終了: {0}", e.ID));
            if (e.Status == CalibrationStatus.OK)
            {
                user.SkeletonCapability.StartTracking(e.ID);
            }
        }

        void SkeletonCapability_CalibrationStart(object sender, CalibrationStartEventArgs e)
        {
            Console.WriteLine(String.Format("キャリブレーション開始: {0}", e.ID));

        }

        void user_NewUser(object sender, NewUserEventArgs e)
        {
            Console.WriteLine(String.Format("ユーザ検出: {0}", e.ID));
            user.SkeletonCapability.RequestCalibration(e.ID, true);
        }

        private void user_Lost(object sender, UserLostEventArgs e)
        {
            Console.WriteLine(String.Format("ユーザ消失: {0}", e.ID));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.shouldRun = false;
            this.readerThread.Join();
            base.OnClosing(e);
        }

        private void ReaderThread()
        {
            while (this.shouldRun)
            {
                this.context.WaitAndUpdateAll();
                DepthMetaData depthMD = depth.GetMetaData();

                this.dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    var users = user.GetUsers();
                    foreach (var u in users)
                    {
                        if (!user.SkeletonCapability.IsTracking(u)) continue;

                        var pointDict = new Dictionary<SkeletonJoint, SkeletonJointPosition>();
                        foreach (SkeletonJoint s in Enum.GetValues(typeof(SkeletonJoint)))
                        {
                            if (!user.SkeletonCapability.IsJointAvailable(s)) continue;
                            pointDict.Add(s, user.SkeletonCapability.GetSkeletonJointPosition(u, s));
                        }

                        this.motionDetector.DetectMotion(u, pointDict);
                    }
                }));
            }
        }
    }
}
