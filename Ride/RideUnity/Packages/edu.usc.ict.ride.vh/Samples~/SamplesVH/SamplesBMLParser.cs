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
