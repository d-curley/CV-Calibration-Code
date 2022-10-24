#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Linq;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;

namespace OpenCVForUnityExample
{
    //[RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class Calibration : MonoBehaviour
    {
        Scalar oneHSVmin;
        Scalar oneHSVmax;
        Scalar twoHSVmin;
        Scalar twoHSVmax;

        public int rows = 30; //make public, control size of squares
        public int cols = 30;
        public double avgThresh = 60;

        public int W = 0;
        public int H = 0;

        public int d = 5;
        public int FreqThresh = 2;

        /// The texture.
        Texture2D texture;
        const int MAX_NUM_OBJECTS = 2;
        //public int MIN_OBJECT_AREA = 900;

        int ball1Y;
        int ball1X;

        int ball2Y;
        int ball2X;

        bool calib=false;

        int width;
        int height;

        Mat rgbMat;
        Mat thresholdMat;
        Mat hsvMat;

        ColorObject one = new ColorObject("one");
        ColorObject two = new ColorObject("two");
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        // Use this for initialization
        void Start()
        {
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();

            //pulls from Color Object Code
            oneHSVmax = one.getHSVmax();
            oneHSVmin = one.getHSVmin();
            twoHSVmax = two.getHSVmax();
            twoHSVmin = two.getHSVmin();
            Debug.Log("one max: " + oneHSVmax);
            Debug.Log("one min: "+ oneHSVmin);
        }

        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(hsvMat, oneHSVmin, oneHSVmax, thresholdMat);
                morphOps(thresholdMat);
                //trackFilteredObject(one, thresholdMat, hsvMat, rgbMat);

                //Second color if needed
                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(hsvMat, twoHSVmin, twoHSVmax, thresholdMat);
                morphOps(thresholdMat);
                //trackFilteredObject(two, thresholdMat, hsvMat, rgbMat);

                Imgproc.rectangle(rgbMat, new Point(W, H), new Point(W+rows, H+cols), new Scalar(0, 200, 0), 2);
                

                //Calibrate HSV
                if (Input.GetKeyDown("1"))
                {
                    oneHSVmax = findHSVmax(hsvMat);
                    oneHSVmin = findHSVmin(hsvMat);
                }

                if (Input.GetKeyDown("2"))
                {
                    twoHSVmax = HSVstdev(hsvMat,true);
                    twoHSVmin = HSVstdev(hsvMat,false);
                }
                if (Input.GetKeyDown("space"))
                {
                    calib = true;
                }
                if (calib)
                {
                    SuggestColor(hsvMat, width, height, rgbMat);
                }
  
                //pulls morph location from trackobject function
                //TargetOne.localPosition = new Vector3(-5f, ball1Y * (-.03f), ball1X * (.03f));
                //TargetTwo.localPosition = new Vector3(-5f, ball2Y * (-.03f), ball2X * (.03f));
                Utils.fastMatToTexture2D(rgbMat, texture);
            }
        }
        /// <param name="thresh">Thresh.</param>
        private void morphOps(Mat thresh)
        {
            //create structuring element that will be used to "dilate" and "erode" image.
            //the element chosen here is a 3px by 3px rectangle
            Mat erodeElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
            //dilate with larger element so make sure object is nicely visible
            Mat dilateElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(8, 8));

            Imgproc.erode(thresh, thresh, erodeElement);
            Imgproc.erode(thresh, thresh, erodeElement);

            Imgproc.dilate(thresh, thresh, dilateElement);
            Imgproc.dilate(thresh, thresh, dilateElement);
        }

        private void SuggestColor(Mat hsv,int h, int w, Mat draw)
        {
            double[] check;
            List<double> hist = new List<double>();
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    check = hsv.get(i,j);
                    hist.Add(check[0]);// hist will be full of pixel H values
                }
            }
            var frequency = hist.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            for(int i = 1; i < 256; i++)
            {
                if (frequency.ContainsKey(i)){ }
                else {frequency.Add(i, 0); }
            }

            var items = from pair in frequency //output this into update
                        orderby pair.Key ascending
                        select pair;

            int total = 0;
            int X = 0;
            
            //run always in update, just need to initialize items, or trigger with boolean that runs after first calibration
            foreach (KeyValuePair<double, int> pair in items)
            {
                total += pair.Value;
                //Debug.Log("H: "+ pair.Key+ "  Freq:  " + pair.Value); 
                if (pair.Value < FreqThresh)
                {
                    float Hconvert = (float)(pair.Key / 255);
                    Color rgb= Color.HSVToRGB(Hconvert, .8f, .75f,true);
                    
                    Scalar color = new Scalar(rgb[0]*255, rgb[1] * 255, rgb[2] * 255);
                    Imgproc.line(draw, new Point(X,400), new Point(X,480), color, d); //color needs to be a scalar
                    X =X + d;
                }
            }
        }



        private Scalar findHSVavg(Mat hsv, bool max)
        {
            double[] average = new double[3];
            double[] check;

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    check = hsv.get(i + W, j + H);
                    for (int x = 0; x < 3; x++)
                    {
                        average[x] = average[x] + check[x] / (rows * cols);
                    }
                }

            if (max)
            {
                Scalar avgHSV = new Scalar(average[0] + avgThresh, average[1] + avgThresh, average[2] + avgThresh);
                Debug.Log("avgmax: " + avgHSV);
                return avgHSV;
            }
            else
            {
                Scalar avgHSV = new Scalar(average[0] - avgThresh, average[1] - avgThresh, average[2] - avgThresh);
                Debug.Log("avgmin: " + avgHSV);
                return avgHSV;
            }
        }


        private Scalar HSVstdev(Mat hsv, bool upperthresh)
        { //https://www.johndcook.com/blog/standard_deviation/
            double meanH=0;
            double LastmeanH= hsv.get(W, H)[0];
            double stdevH=0;
            double[] max = new double[3];
            double[] min = hsv.get(W,H);
            double[] check;

            for (int i = 1; i < rows; i++)
            {
                for (int j = 1; j < cols; j++)
                {
                    check = hsv.get(i + W, j + H);
                    meanH = LastmeanH + (check[0] - LastmeanH) / (i+j);

                    stdevH = stdevH + (check[0] - LastmeanH) * (check[0] - meanH);

                    LastmeanH = meanH;

                    if (check[1] > max[1]) { max[1] = check[1]; } //Smax
                    if (check[2] > max[2]) { max[2] = check[2]; }//Vmax

                    if (check[1] < min[1]) { min[1] = check[1]; } //Smin
                    if (check[2] < min[2]) { min[2] = check[2]; }//Vmin
                }
            }
            stdevH = Mathf.Sqrt((float)stdevH / (rows * cols - 1));
            Debug.Log("stdevH: " + stdevH);
            if (upperthresh)
            {
                Scalar stdHSV = new Scalar(LastmeanH + stdevH,max[1],max[2]);
                Debug.Log("stdevmax: " + stdHSV);
                return stdHSV;
            }
            else
            {
                Scalar stdHSV = new Scalar(LastmeanH - stdevH, min[1], min[2]);
                Debug.Log("stdevmin: " + stdHSV);
                return stdHSV;
            } 
        }

        private Scalar findHSVmax(Mat hsv) //need to create subset mat in target area
        {
            double[] max = new double[3];
            double[] check;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    check = hsv.get(i + W, j + H);

                    if (check[0] > max[0]) { max[0] = check[0]; }
                    if (check[1] > max[1]) { max[1] = check[1]; }
                    if (check[2] > max[2]) { max[2] = check[2]; }
                }
            }
                
            Scalar maxHSV = new Scalar(max[0], max[1], max[2]);
            Debug.Log("max: "+maxHSV);
            return maxHSV;
        }

        private Scalar findHSVmin(Mat hsv) //need to create subset mat in target area
        {
            double[] min = hsv.get(W, H);
            double[] check;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    check = hsv.get(i + W, j + H);

                    if (check[0] < min[0]) { min[0] = check[0]; }
                    if (check[1] < min[1]) { min[1] = check[1]; }
                    if (check[2] < min[2]) { min[2] = check[2]; }
                }
            }

            Scalar minHSV = new Scalar(min[0], min[1], min[2]);
            Debug.Log("min: "+minHSV);
            return minHSV;
        }


        /// <param name="theColorObject">The color object.</param>
        /// <param name="threshold">Threshold.</param>
        /// <param name="HSV">HS.</param>
        /// <param name="cameraFeed">Camera feed.</param>
        private void trackFilteredObject(ColorObject theColorObject, Mat threshold, Mat HSV, Mat cameraFeed)
        {
            List<ColorObject> colorObjects = new List<ColorObject>();
            Mat temp = new Mat();
            threshold.copyTo(temp);
            //these two vectors needed for output of findContours
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();
            //find contours of filtered image using openCV findContours function
            Imgproc.findContours(temp, contours, hierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_SIMPLE);

            //use moments method to find our filtered object
            bool colorObjectFound = false;
            if (hierarchy.rows() > 0)
            {
                int numObjects = hierarchy.rows();
                //if number of objects greater than MAX_NUM_OBJECTS we have a noisy filter
                if (numObjects < MAX_NUM_OBJECTS)
                {
                    for (int index = 0; index >= 0; index = (int)hierarchy.get(0, index)[0])
                    {

                        Moments moment = Imgproc.moments(contours[index]);
                        double area = moment.get_m00();

                        //if the area is less than 20 px by 20px then it is probably just noise
                        //if the area is the same as the 3/2 of the image size, probably just a bad filter
                        //we only want the object with the largest area so we safe a reference area each
                        //iteration and compare it to the area in the next iteration.
                        if (area > (rows-15)*(cols - 15))
                        {
                            ColorObject colorObject = new ColorObject();
                            colorObject.setXPos((int)(moment.get_m10() / area));
                            colorObject.setYPos((int)(moment.get_m01() / area));
                            colorObject.setType(theColorObject.getType());
                            colorObject.setColor(theColorObject.getColor());
                            colorObjects.Add(colorObject);
                            colorObjectFound = true;
                        }
                        else
                        {
                            colorObjectFound = false;
                        }
                    }
                }
                else
                {
                    Imgproc.putText(cameraFeed, "TOO MUCH NOISE!", new Point(5, cameraFeed.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
            }
        }


        public void OnWebCamTextureToMatHelperInitialized()
        {
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGB24, false);
            Utils.fastMatToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);

            width = webCamTextureMat.width();
            height = webCamTextureMat.height();
            print(width);

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;

            if (widthScale < heightScale)
            { Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2; }
            else { Camera.main.orthographicSize = height / 2; }
            rgbMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
            thresholdMat = new Mat();
            hsvMat = new Mat();
        }

        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (rgbMat != null)
                rgbMat.Dispose();
            if (thresholdMat != null)
                thresholdMat.Dispose();
            if (hsvMat != null)
                hsvMat.Dispose();
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }


        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
        }
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }
    }
}

#endif