using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VHAssets;


namespace Ride.Samples
{
    public class SamplesBMLParser : RideMonoBehaviour
    {
        public MecanimCharacter m_character;

        private DebugMenu m_debugMenu;
        private string bmlTextArea = "";

        private readonly (string, string) [] bmlSamples = 
        {
            // Anim
            ( "anim1", @"<animation name=""IdleStandingUpright01_ChopLf01""/>" ),
            ( "anim2", @"<animation name=""IdleStandingUpright01_ChopLf01"" stroke=""1""/>" ),
            ( "anim3", @"<animation name=""IdleStandingUpright01_ChopLf01"" start=""1"" stroke=""2""/>" ),
            ( "anim4", @"<animation name=""IdleStandingUpright01_ChopLf01"" start=""1"" ready=""1.5"" stroke=""4""/>" ),
            // Nod/Shake
            ( "nod1", @"<head type=""NOD""/>" ),
            ( "nod2", @"<head type=""NOD"" repeats=""2""/>" ),
            ( "nod3", @"<head type=""NOD"" velocity="".5""/>" ),
            ( "nod4", @"<head type=""NOD"" amount="".8""/>" ),
            ( "shake1", @"<head type=""SHAKE"" start=""0"" end=""3""/>" ),
            // Gaze
            ( "gaze1", @"<gaze target=""Camera""/>" ),
            //@"<gaze target=""Main Camera:Offset""/>" ),
            //( "gaze2", @"<gaze target=""Camera"" sbm:joint-range=""NECK EYES""/>" ),
            //( "gaze3", @"<gaze target=""Camera"" sbm:joint-range=""CHEST BACK""/>" ),
            //( "gaze4", @"<gaze target=""Camera"" sbm:joint-speed=""500""/>" ),
            //( "gaze5", @"<gaze target=""Camera"" sbm:joint-speed=""800 1500""/>" ),
            //( "gaze6", @"<gaze target=""Camera"" start=""2""/>" ),
            // ID examples
            //( "animid", @"<animation id=anim1 name=""IdleStandingUpright01_ChopLf01"" stroke=""1""/><animation id=anim2 name=""IdleStandingUpright01_MeLf01"" start=""anim1:relax""/>" ),
            // Math
            //( "math", @"<animation id=anim1 name=""IdleStandingUpright01_ChopLf01"" stroke=""1""/><animation id=anim2 name=""IdleStandingUpright01_MeLf01"" start=""anim1:relax+2""/>" ),
            // Test
            //( "social_smile", @"<face amount=""0.7"" au=""12"" end=""1.8"" side=""BOTH"" start=""0.0"" ready=""0.5"" relax=""1.2"" type=""facs""/>" ),
            //( "big_nod", @"<act><face id=""blink"" type=""facs"" au=""45"" amount=""0.4"" start=""0"" end=""0.1"" side=""BOTH"" group=""big_nod""/>
            //    <head id=""anticipation"" type=""nod"" velocity=""1"" amount=""-0.02"" repeats=""0.5"" start=""blink:start"" group=""big_nod""/>
            //    <head id=""action"" type=""nod"" velocity=""0.8"" amount=""0.1"" repeats=""1"" start=""anticipation:relax"" relax=""anticipation:relax+0.8"" group=""big_nod""/>
            //    <head id=""overshoot"" type=ang""nod"" velocity=""0.8"" amount=""0.05"" repeats=""0.5"" start=""action:relax"" group=""big_nod""/></act>" ),
            //( "angry", @"<act><face amount=""1"" au=""4"" end=""6.0"" relax=""2.0"" side=""BOTH"" start=""0"" stroke="".4"" type=""facs""/>
            //    <face amount=""0.4"" au=""5"" end=""6.0"" relax=""2.0"" side=""BOTH"" start=""0"" stroke="".4"" type=""facs""/>
            //    <face amount=""1"" au=""10"" end=""6.0"" relax=""2.0"" side=""BOTH"" start=""0"" stroke="".4"" type=""facs""/></act>" ),
            //( "toss_nod", @"<act><head amount=""0.15"" start=""0.2"" relax=""0.7"" repeats=""0.5"" type=""NOD"" group=""toss_nod""/>
            //    <head type=""TOSS"" amount=""0.20"" repeats=""0.5"" start=""0"" ready=""0.2"" end=""0.7"" group=""toss_nod""/></act>" ),
            //( "simple shake", @"<head id=""action"" type=""shake"" amount=""0.1"" repeats=""1"" start=""0"" group=""shake""/>"),
            //( "combined shake", @"<act><head id=""action"" type=""shake"" amount=""0.1"" repeats=""1"" start=""0"" group=""shake""/>
            //    <head id=""overshoot"" type=""shake"" amount=""0.03"" repeats=""0.5"" start=""action:relax"" group=""shake""/></act>"),
            // Gesture
            //<gesture id="a" lexeme="DEICTIC" type="LEFT" stroke="2" relax="4">
            //<gesture id="b" lexeme="DEICTIC" type="MID" start="a:relax" ready="a:relax" stroke_start="a:relax" stroke="6" relax="8"/>
            //<gesture id="c" lexeme="DEICTIC" type="RIGHT" start="b:relax" ready="b:relax" stroke_start="b:relax" stroke="10"/>
            //<gesture id="a" lexeme="DEICTIC" type="LEFT" stroke="2" relax="4">
            //<gesture id="b" lexeme="DEICTIC" type="MID" stroke="a:relax" relax="6"/>
            //<gesture id="c" lexeme="DEICTIC" type="RIGHT" stroke="b:relax" relax="8"/>
            //<gesture id="a" lexeme="DEICTIC" type="LEFT" stroke="2" relax="4" sbm:joint-range="l_shoulder" sbm:frequency="0.03" sbm:scale="0.02"/>
        };

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();
        }


        public void OnGUIBMLParser()
        {
            m_debugMenu.Label("<b>BML</b>");

            int numPerRow = 3;
            for (int i = 0; i < bmlSamples.Length; i += numPerRow)
            {
                using (m_debugMenu.Horizontal())
                {
                    for (int j = 0; j < numPerRow; j++)
                    {
                        int index = i + j;
                        if (index >= bmlSamples.Length)
                            break;

                        if (m_debugMenu.Button(bmlSamples[index].Item1))
                        {
                            m_character.PlayXml(bmlSamples[index].Item2);
                            bmlTextArea = bmlSamples[index].Item2;
                        }
                    }
                }
            }

            m_debugMenu.TextArea(bmlTextArea);
        }
    }
}
