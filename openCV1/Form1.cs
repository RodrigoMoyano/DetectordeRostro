using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Face;
using Emgu.CV.CvEnum;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace openCV1
{
    public partial class Form1 : Form
    {
        int testid = 0;
        private Capture videoCapture = null;
        private Image<Bgr, Byte> currentFrame = null;
        Mat frame = new Mat();
        private bool facesDetectionEnable = false;
        CascadeClassifier faceCasacdeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        Image<Bgr, Byte> faceResult = null;
        List<Image<Gray,Byte>> TrainedFaces = new List<Image<Gray,byte>>();
        List<int> PersonsLabes = new List<int>();

        bool EnableSaveImage = false;
        private bool isTrained = false;
        EigenFaceRecognizer recognizer;
        List<string> PersonsNames = new List<string>();


        public Form1()
        {
            InitializeComponent();
        }

        private void btnCapure_Click(object sender, EventArgs e)
        {
            if(videoCapture != null) videoCapture.Dispose();
            videoCapture = new Capture();
            //videoCapture.ImageGrabbed += ProcessFrame;
            Application.Idle += ProcessFrame;
            //videoCapture.Start();

        }

        private void ProcessFrame(object sender, EventArgs e)
        {
          if(videoCapture != null && videoCapture.Ptr != IntPtr.Zero)
            {
                videoCapture.Retrieve(frame, 0);
                currentFrame = frame.ToImage<Bgr, Byte>().Resize(picCapture.Width, picCapture.Height, Inter.Cubic);

            
            

            if (facesDetectionEnable)
            {
                Mat grayImage = new Mat();
                CvInvoke.CvtColor(currentFrame, grayImage, ColorConversion.Bgr2Gray);
                CvInvoke.EqualizeHist(grayImage, grayImage);

                Rectangle[] faces = faceCasacdeClassifier.DetectMultiScale(grayImage, 1.1, 3,Size.Empty, Size.Empty);
                
                if(faces.Length > 0)
                {
                    foreach (var face in faces)
                    {
                        //CvInvoke.Rectangle(currentFrame, face,new Bgr(Color.Red).MCvScalar, 2);

                        Image<Bgr, Byte> resultImage = currentFrame.Convert<Bgr, Byte>();
                        resultImage.ROI = face;
                        picDetected.SizeMode = PictureBoxSizeMode.StretchImage;
                        picDetected.Image = resultImage.Bitmap;

                        if (EnableSaveImage)
                        {
                            string path = Directory.GetCurrentDirectory() + @"\TreainedImages";
                            if(!Directory.Exists(path))
                                Directory.CreateDirectory(path);

                                Task.Factory.StartNew(() =>
                                {
                                    for (int i = 0; i < 10; i++)
                                    {

                                        resultImage.Resize(200, 200, Inter.Cubic).Save(path + @"\" + txtPersonName.Text +"_"+ DateTime.Now.ToString("dd-mm-yyyy-hh-ss") + ".jpg");
                                        Thread.Sleep(1000);
                                    }
                                });
                        }
                        EnableSaveImage = false;

                       if (btnAddPerson.InvokeRequired)
                        {
                                btnAddPerson.Invoke(new ThreadStart(delegate {

                                    btnAddPerson.Enabled = true;
                                }));
                        }             
                        if (isTrained)
                        {
                            Image<Gray, Byte> grayFaceResult = resultImage.Convert<Gray, Byte>().Resize(200,200,Inter.Cubic);
                            CvInvoke.EqualizeHist(grayFaceResult, grayFaceResult);
                            var result = recognizer.Predict(grayFaceResult);
                            pictureBox1.Image = grayFaceResult.Bitmap;
                            pictureBox2.Image = TrainedFaces[result.Label].Bitmap;
                            Debug.WriteLine(result.Label + "." + result.Distance);

                            if(result.Label != -1 && result.Distance < 2000)
                            {
                                CvInvoke.PutText(currentFrame, PersonsNames[result.Label], new Point(face.X - 2, face.Y - 2),
                                    FontFace.HersheyComplex, 1.0, new Bgr(Color.Orange).MCvScalar);
                                CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Green).MCvScalar, 2);
                            }
                            else
                            {
                                CvInvoke.PutText(currentFrame, "Unknown", new Point(face.X - 2, face.Y - 2),
                                    FontFace.HersheyComplex , 1.0 , new Bgr(Color.Orange).MCvScalar);
                                    CvInvoke.Rectangle(currentFrame, face, new Bgr(Color.Green).MCvScalar, 2);
                            }


                        }
                    }
                }
            }
            
            picCapture.Image = currentFrame.Bitmap;
          }

          if(currentFrame != null)
                currentFrame.Dispose();

        }

        private void btnDetectFaces_Click(object sender, EventArgs e)
        {
            facesDetectionEnable = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            btnSave.Enabled = false;
            btnAddPerson.Enabled = true;
            EnableSaveImage = false;
        }
        private void btnAddPerson_Click(object sender, EventArgs e)
        {
            
            btnAddPerson.Enabled = false;
            EnableSaveImage = true;
        }

        private void btnTrain_Click(object sender, EventArgs e)
        {
            TrainImagesFromDir();
        }
        private bool TrainImagesFromDir()
        {
            int ImagesCount = 0;
            double Threshold = 20000;
            TrainedFaces.Clear();
            PersonsLabes.Clear();
            PersonsNames.Clear();
            try
            {
                string path = Directory.GetCurrentDirectory() + @"\TrainedImages";
                string[] files = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories); ;

                foreach (var file in files)
                {
                    Image<Gray, Byte> trainedImage = new Image<Gray, byte>(file).Resize(200,200,Inter.Cubic);
                    CvInvoke.EqualizeHist(trainedImage, trainedImage);
                    TrainedFaces.Add(trainedImage);
                    PersonsLabes.Add(ImagesCount);
                    string name = file.Split('\\').Last().Split('_')[0];
                    PersonsNames.Add(name);
                    ImagesCount++;
                    Debug.WriteLine(ImagesCount + "." +name);
                }

                if(TrainedFaces.Count() > 0)
                {

                

                    recognizer = new EigenFaceRecognizer(ImagesCount, Threshold);
                    recognizer.Train(TrainedFaces.ToArray(), PersonsLabes.ToArray());
                
                    isTrained = true;
                    //Debug.WriteLine(ImagesCount);
                    //Debug.WriteLine(isTrained);
                    return true;
                }
                else
                {
                    isTrained= false;
                    return false;
                }

            }
            catch (Exception ex) 
            {

                isTrained = false;
                MessageBox.Show("Error in Trein Images: " + ex.Message);
                return false;
            }
            
        }

        

        
    }
}
