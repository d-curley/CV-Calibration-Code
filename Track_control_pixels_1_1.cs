#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

namespace OpenCVForUnityExample
{
    //[RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class Track_control_pixels_1_1 : MonoBehaviour
    {
        private Vector3 Displacement;
        public Transform RightShoulder;
        public Transform RightTarget;
        private Transform Shoulder;
        private Transform Target;
        private float ArmLength = 8f;

        Dictionary<double, int> Hcount = new Dictionary<double, int>();
        Dictionary<double, int> Vcount = new Dictionary<double, int>();
        Dictionary<double, int> Scount = new Dictionary<double, int>();

        double[] max;
        double[] min;

        Scalar redHSVmin;
        Scalar redHSVmax;
        Scalar greenHSVmin;
        Scalar greenHSVmax;

        int rows = 50; //make public, control size of squares
        int cols = 50;

        bool pause=false;

        /// The texture.
        Texture2D texture;
        const int MAX_NUM_OBJECTS = 2;
        const int MIN_OBJECT_AREA = 30 * 30;//maybe set to rows *cols

        int ball1Y;
        int ball1X;

        //int ball2Y;
        //int ball2X;

        Mat rgbMat;
        Mat thresholdMat;
        Mat hsvMat;

        ColorObject red = new ColorObject("green");
        //ColorObject green = new ColorObject("green");
        WebCamTextureToMatHelper webCamTextureToMatHelper;
        FpsMonitor fpsMonitor;

        float width;
        float height;

        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();

            //pulls from Color Object Code
            redHSVmax = red.getHSVmax(); 
            redHSVmin = red.getHSVmin();
            //greenHSVmax = green.getHSVmax();
            //greenHSVmin = green.getHSVmin();
        }

        void Update()
        {
            //Second color if needed
            //Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
            //Core.inRange(hsvMat, greenHSVmin, greenHSVmax, thresholdMat);
            //morphOps(thresholdMat);
            //trackFilteredObject(green, thresholdMat, hsvMat, rgbMat);
            if (pause)
            {
                
            }
            else
            {
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();
                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(hsvMat, redHSVmin, redHSVmax, thresholdMat);
                morphOps(thresholdMat);
                trackFilteredObject(red, thresholdMat, hsvMat, rgbMat);
                Imgproc.rectangle(rgbMat, new Point(0, 0), new Point(rows, cols), new Scalar(0, 200, 0), 2);
                Utils.fastMatToTexture2D(rgbMat, texture);
            }
            //Calibrate HSV
            if (Input.GetKeyDown(KeyCode.P)){ //recommend round objects; flat ones often change reflection
                Debug.Log("Calibrating, please wait");
                findHSVRange(hsvMat);
                //bkacground updates for max and muin
                Debug.Log("Done Calibrating (for now)");
                Debug.Log(redHSVmax);
                Debug.Log(redHSVmin);
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log("Recalibrating");
                HSVrangeCompression();
                Debug.Log("Done realibrating (for now)");
                Debug.Log(redHSVmax);
                Debug.Log(redHSVmin);
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                pause = !pause;
                SuggestColor(hsvMat, rgbMat);
                Utils.fastMatToTexture2D(rgbMat, texture);
            }
            
            //pulls morph location from trackobject function
            RightTarget.localPosition = new Vector3(0f, ball1Y * (-.03f), ball1X * (.03f));

            Vector3 directionR = RightTarget.localPosition - RightShoulder.localPosition;
            float distanceR = (directionR).sqrMagnitude;
            if (distanceR > ArmLength * ArmLength)
            {
                RightTarget.localPosition -= directionR * .05f;
            }

            //Second target, if required
            //LeftTarget.localPosition = new Vector3(0f, ball2Y * (-.03f), ball2X * (.03f));

            //Vector3 directionL = LeftTarget.localPosition - LeftShoulder.localPosition;
            //float distanceL = (directionL).sqrMagnitude;
            //if (distanceL > ArmLength * ArmLength)
            //{
            //    LeftTarget.localPosition -= directionL * .05f;
            //}
            

        }

        private void SuggestColor(Mat hsv, Mat draw)
        {
            double[] check;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    check = hsv.get(i, j);

                    if (Hcount.ContainsKey(check[0])) { Hcount[check[0]]++; }
                    else { Hcount.Add(check[0], 1); }

                    if (Scount.ContainsKey(check[1])) { Scount[check[1]]++; }
                    else { Scount.Add(check[1], 1); }

                    if (Vcount.ContainsKey(check[2])) { Vcount[check[2]]++; }
                    else { Vcount.Add(check[2], 1); }
                }
            }
            int X = 0;
            //can we organize by hue?
            //run always in update, just need to initialize items, or trigger with boolean that runs after first calibration
            foreach (KeyValuePair<double, int> pair in Hcount)
            {
                if (pair.Value < 100)//low frequency
                {
                    float Hconvert = (float)(pair.Key / 255);
                    Color rgb = Color.HSVToRGB(Hconvert, .8f, .75f, true);

                    Scalar color = new Scalar(rgb[0] * 255, rgb[1] * 255, rgb[2] * 255);
                    Imgproc.line(draw, new Point(X, (height-80)), new Point(X, height), color, 8); //color needs to be a scalar
                    X = X + 8;
                }
            }
        }
        


        private void findHSVRange(Mat hsv)
        {
            max = hsv.get(0, 0); //for comparison
            double[] check;
            min = hsv.get(0, 0); //for comparison

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {//row and col --> W and H as starting point
                    check = hsv.get(i, j);//get(i+W,j+H) where W and H are the starting location

                    //max
                    if (check[0] > max[0]) { max[0] = check[0]; }//H
                    if (check[1] > max[1]) { max[1] = check[1]; }//S
                    if (check[2] > max[2]) { max[2] = check[2]; }//V

                    //min
                    if (check[0] < min[0]) { min[0] = check[0]; }
                    if (check[1] < min[1]) { min[1] = check[1]; }
                    if (check[2] < min[2]) { min[2] = check[2]; }
                }
            }
            redHSVmax = new Scalar(max[0], max[1], max[2]);//maybe I don't need these, just max and min
            redHSVmin = new Scalar(min[0], min[1], min[2]);

            //hardcode the pixel numbers for now
            for (int i = rows; i < 480; i++)
            {
                for (int j = cols; j < 640 ; j++)
                {
                    check = hsv.get(i, j);
                    if(check[0]<max[0]&&check[0]>min[0]&& check[1] < max[1] && check[1] > min[1]&& check[2] < max[2] && check[2] > min[2])
                    {
                        if (Hcount.ContainsKey(check[0])) { Hcount[check[0]]++; }
                        else { Hcount.Add(check[0], 1); }

                        if (Scount.ContainsKey(check[1])) { Scount[check[1]]++; }
                        else { Scount.Add(check[1], 1); }

                        if (Vcount.ContainsKey(check[2])) { Vcount[check[2]]++; }
                        else { Vcount.Add(check[2], 1); }
                    }
                }      
            }
        }

        private void HSVrangeCompression()
        {
            KeyValuePair<double, int> HmaxCurrent = new KeyValuePair<double, int>(0, 0);
            foreach (KeyValuePair<double, int> maxCheck in Hcount){ if (maxCheck.Value > HmaxCurrent.Value) HmaxCurrent = maxCheck;}
            if ((HmaxCurrent.Key - min[0]) < (max[0] - HmaxCurrent.Key)){ min[0] = HmaxCurrent.Key;} //Check if you are closer to the min or max
            else{ max[0] = HmaxCurrent.Key; }
            Hcount.Remove(HmaxCurrent.Key);

            KeyValuePair<double, int> SmaxCurrent = new KeyValuePair<double, int>(0, 0);
            foreach (KeyValuePair<double, int> maxCheck in Scount) { if (maxCheck.Value > SmaxCurrent.Value) SmaxCurrent = maxCheck; }
            if ((SmaxCurrent.Key - min[1]) < (max[1] - SmaxCurrent.Key)) { min[1] = SmaxCurrent.Key; }
            else { max[1] = SmaxCurrent.Key; }
            Scount.Remove(SmaxCurrent.Key);

            KeyValuePair<double, int> VmaxCurrent = new KeyValuePair<double, int>(0, 0);
            foreach (KeyValuePair<double, int> maxCheck in Vcount) { if (maxCheck.Value > VmaxCurrent.Value) VmaxCurrent = maxCheck; }
            if ((VmaxCurrent.Key - min[2]) < (max[2] - VmaxCurrent.Key)) { min[2] = VmaxCurrent.Key; }
            else { max[2] = VmaxCurrent.Key; }
            Vcount.Remove(VmaxCurrent.Key);

            redHSVmax = new Scalar(max[0], max[1], max[2]);//maybe I don't need these, just max and min
            redHSVmin = new Scalar(min[0], min[1], min[2]);
        }

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
        /// <param name="thresh">Thresh.</param>
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
                        if (area > MIN_OBJECT_AREA)
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
                    //let user know you found an object
                    if (colorObjectFound == true)
                    {
                        //draw object location on screen
                        //drawObject(colorObjects, cameraFeed, temp, contours, hierarchy);
                        if (theColorObject == red)
                        {
                            ball1X = (colorObjects[0].getXPos() - 320);
                            ball1Y = (colorObjects[0].getYPos() - 240);
                        }
                        //if (theColorObject == green)
                        //{
                        //    ball2X = (colorObjects[0].getXPos() - 320);
                        //    ball2Y = (colorObjects[0].getYPos() - 240);
                        //    Debug.Log("X: " + ball2X + " // Y: " + ball2Y);
                        //}
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