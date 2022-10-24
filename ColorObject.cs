using UnityEngine;
using System.Collections;
using OpenCVForUnity.CoreModule;

namespace OpenCVForUnityExample
{
    public class ColorObject
    {
        int xPos, yPos;
        string type;
        Scalar HSVmin, HSVmax;
        Scalar Color;

        public ColorObject ()
        {
            //set values for default constructor
            setType ("Object");
            setColor (new Scalar (0, 0, 0));
        }

        public ColorObject (string name)
        {
            setType (name);
            if (name == "red") {
                //morning
                //setHSVmin (new Scalar (0, 180, 100));
                //setHSVmax (new Scalar (15, 255, 200));
                //afternoon
                setHSVmin(new Scalar(120, 120, 90));
                setHSVmax(new Scalar(190, 255, 255));
            }
            if (name == "green")
            {
                //morning
                //setHSVmin (new Scalar (23, 91, 0));
                //setHSVmax (new Scalar (91, 255, 127));
                //afternoon
               // setHSVmin(new Scalar(70, 53, 0));
                //setHSVmax(new Scalar(111, 255, 55));
                //night green
                setHSVmin(new Scalar(60, 88, 12));
                setHSVmax(new Scalar(95,181, 72));
            }
            if (name == "one")
            {
                setHSVmin (new Scalar (0, 180, 100));
                setHSVmax (new Scalar (15, 255, 200));
            }
            if (name == "two")
            {
                setHSVmin(new Scalar(120, 120, 90));
                setHSVmax(new Scalar(190, 255, 255));
            }
            if (name == "three")
            {
                setHSVmin(new Scalar(120, 120, 90));
                setHSVmax(new Scalar(190, 255, 255));
            }
            if (name == "four")
            {
                setHSVmin(new Scalar(120, 120, 90));
                setHSVmax(new Scalar(190, 255, 255));
            }

        }
        public int getXPos () { return xPos;}

        public void setXPos (int x) {xPos = x; }

        public int getYPos (){return yPos;}

        public void setYPos (int y) {yPos = y;}

        public Scalar getHSVmin () {return HSVmin;}

        public Scalar getHSVmax (){return HSVmax;}

        public void setHSVmin (Scalar min){ HSVmin = min;}

        public void setHSVmax (Scalar max) {HSVmax = max;}

        public string getType () {return type;}

        public void setType (string t) {type = t; }

        public Scalar getColor (){ return Color; }

        public void setColor (Scalar c){ Color = c; }
    }
}