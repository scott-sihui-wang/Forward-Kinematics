 /* 
BVH Tools for Unity/VRM
Copyright (c) 2019 Emiliana (twitter.com/Emiliana_vt)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
https://github.com/emilianavt/BVHTools
 */

using System;
using System.Collections.Generic;

// This class uses no Unity data types and should be completely safe to use in another thread
public class BVHParser {
    public int frames = 0;
    public float frameTime = 1000f / 60f;
    public BVHBone root;
    private List<BVHBone> boneList;

    static private char[] charMap = null;
    private float[][] channels;
    private string bvhText;
    private int pos = 0;

    public class BVHBone {
        public string name;
        public List<BVHBone> children;
        public float offsetX, offsetY, offsetZ;
        public int[] channelOrder;
        public int channelNumber;
        public BVHChannel[] channels;

        private BVHParser bp;

        // 0 = Xpos, 1 = Ypos, 2 = Zpos, 3 = Xrot, 4 = Yrot, 5 = Zrot
        public struct BVHChannel {
            public bool enabled;
            public float[] values;
        }

        public enum BVHJointType {
            ROOT,
            JOINT,
            END
        }

        public BVHBone(BVHParser parser, BVHJointType jointType) {
            bp = parser;
            bp.boneList.Add(this);
            channels = new BVHChannel[6];
            channelOrder = new int[6] { 0, 1, 2, 5, 3, 4 };
            children = new List<BVHBone>();

            bp.skip();
            if (jointType == BVHJointType.ROOT) {
                bp.assureExpect("ROOT");
            } else if (jointType == BVHJointType.JOINT) {
                bp.assureExpect("JOINT");
            } else {
                bp.assureExpect("END");
            }

            bp.assure("joint name", bp.getString(out name));
            bp.skip();
            bp.assureExpect("{");
            bp.skip();
            bp.assureExpect("OFFSET");
            bp.skip();
            bp.assure("offset X", bp.getFloat(out offsetX));
            bp.skip();
            bp.assure("offset Y", bp.getFloat(out offsetY));
            bp.skip();
            bp.assure("offset Z", bp.getFloat(out offsetZ));
            bp.skip();

            if (jointType != BVHJointType.END) 
            {
                bp.assureExpect("CHANNELS");

                bp.skip();
                bp.assure("channel number", bp.getInt(out channelNumber));
                bp.assure("valid channel number", channelNumber >= 1 && channelNumber <= 6);

                for (int i = 0; i < channelNumber; i++)
                {
                    bp.skip();
                    int channelId;
                    bp.assure("channel ID", bp.getChannel(out channelId));
                    channelOrder[i] = channelId;
                    channels[channelId].enabled = true;
                }
            }

            char peek = ' ';
            do {
                bp.skip();
                bp.assure("child joint", bp.peek(out peek));
                switch (peek) {
                    case 'J':
                        BVHBone child = new BVHBone(bp, BVHJointType.JOINT);
                        children.Add(child);
                        break;
                    case 'E':
                        child = new BVHBone(bp, BVHJointType.END);
                        children.Add(child);
                        break;
                    case '}':
                        bp.assureExpect("}");
                        break;
                    default:
                        bp.assure("child joint", false);
                        break;
                }
            } while (peek != '}');
        }
    }

    private bool peek(out char c) {
        c = ' ';
        if (pos >= bvhText.Length) {
            return false;
        }
        c = bvhText[pos];
        return true;
    }

    private bool expect(string text) {
        foreach (char c in text) {
            if (pos >= bvhText.Length || (c != bvhText[pos] && bvhText[pos] < 256 && c != charMap[bvhText[pos]])) {
                return false;
            }
            pos++;
        }
        return true;
    }

    private bool getString(out string text) {
        text = "";
        while (pos < bvhText.Length && bvhText[pos] != '\n' && bvhText[pos] != '\r') {
            text += bvhText[pos++];
        }
        text = text.Trim();

        return (text.Length != 0);
    }

    private bool getChannel(out int channel) {
        channel = -1;
        if (pos + 1 >= bvhText.Length) {
            return false;
        }
        switch (bvhText[pos]) {
            case 'x':
            case 'X':
                channel = 0;
                break;
            case 'y':
            case 'Y':
                channel = 1;
                break;
            case 'z':
            case 'Z':
                channel = 2;
                break;
            default:
                return false;
        }
        pos++;
        switch (bvhText[pos]) {
            case 'p':
            case 'P':
                pos++;
                return expect("osition");
            case 'r':
            case 'R':
                pos++;
                channel += 3;
                return expect("otation");
            default:
                return false;
        }
    }

    private bool getInt(out int v) {
        bool negate = false;
        bool digitFound = false;
        v = 0;

        // Read sign
        if (pos < bvhText.Length && bvhText[pos] == '-') {
            negate = true;
            pos++;
        } else if (pos < bvhText.Length && bvhText[pos] == '+') {
            pos++;
        }

        // Read digits
        while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9') {
            v = v * 10 + (int)(bvhText[pos++] - '0');
            digitFound = true;
        }

        // Finalize
        if (negate) {
            v *= -1;
        }
        if (!digitFound) {
            v = -1;
        }
        return digitFound;
    }

    // Accuracy looks okay
    private bool getFloat(out float v) {
        bool negate = false;
        bool digitFound = false;
        int i = 0;
        v = 0f;

        // Read sign
        if (pos < bvhText.Length && bvhText[pos] == '-') {
            negate = true;
            pos++;
        } else if (pos < bvhText.Length && bvhText[pos] == '+') {
            pos++;
        }

        // Read digits before decimal point
        while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9') {
            v = v * 10 + (float)(bvhText[pos++] - '0');
            digitFound = true;
        }

        // Read decimal point
        if (pos < bvhText.Length && (bvhText[pos] == '.' || bvhText[pos] == ',')) {
            pos++;

            // Read digits after decimal
            float fac = 0.1f;
            while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9' && i < 128) {
                v += fac * (float)(bvhText[pos++] - '0');
                fac *= 0.1f;
                digitFound = true;
            }
        }

        // Finalize
        if (negate) {
            v *= -1f;
        }
        if (!digitFound) {
            v = float.NaN;
        }
        
        // Parse scientific notation
        if (digitFound && pos < bvhText.Length && (bvhText[pos] == 'e' || bvhText[pos] == 'E'))
        {
            pos++;

            int scientificSign = 1;
            if (pos < bvhText.Length && bvhText[pos] == '-')
            {
                scientificSign = -1;
                pos++;
            }
            else if (pos < bvhText.Length && bvhText[pos] == '+')
            {
                pos++;
            }

            int power = 0;
            while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9')
            {
                power = power * 10 + (bvhText[pos++] - '0');
            }

            power *= scientificSign;

            v *= (float)Math.Pow(10, power);
        }
        
        return digitFound;
    }

    private void skip() {
        while (pos < bvhText.Length && (bvhText[pos] == ' ' || bvhText[pos] == '\t' || bvhText[pos] == '\n' || bvhText[pos] == '\r')) {
            pos++;
        }
    }

    private void skipInLine() {
        while (pos < bvhText.Length && (bvhText[pos] == ' ' || bvhText[pos] == '\t')) {
            pos++;
        }
    }

    private void newline() {
        bool foundNewline = false;
        skipInLine();
        while (pos < bvhText.Length && (bvhText[pos] == '\n' || bvhText[pos] == '\r')) {
            foundNewline = true;
            pos++;
        }
        assure("newline", foundNewline);
    }

    private void assure(string what, bool result) {
        if (!result) {
            string errorRegion = "";
            for (int i = Math.Max(0, pos - 15); i < Math.Min(bvhText.Length, pos + 15); i++) {
                if (i == pos - 1) {
                    errorRegion += ">>>";
                }
                errorRegion += bvhText[i];
                if (i == pos + 1) {
                    errorRegion += "<<<";
                }
            }
            throw new ArgumentException("Failed to parse BVH data at position " + pos + ". Expected " + what + " around here: " + errorRegion);
        }
    }

    private void assureExpect(string text) {
        assure(text, expect(text));
    }

    private void parse(bool overrideFrameTime, float time) {
        // Prepare character table
        if (charMap == null) {
            charMap = new char[256];
            for (int i = 0; i < 256; i++) {
                if (i >= 'a' && i <= 'z') {
                    charMap[i] = (char)(i - 'a' + 'A');
                } else if (i == '\t' || i == '\n' || i == '\r') {
                    charMap[i] = ' ';
                } else {
                    charMap[i] = (char)i;
                }
            }
        }

        // Parse skeleton
        skip();
        assureExpect("HIERARCHY");

        boneList = new List<BVHBone>();
        root = new BVHBone(this, BVHBone.BVHJointType.ROOT);

        // Parse meta data
        skip();
        assureExpect("MOTION");
        skip();
        assureExpect("FRAMES:");
        skip();
        assure("frame number", getInt(out frames));
        skip();
        assureExpect("FRAME TIME:");
        skip();
        assure("frame time", getFloat(out frameTime));

        if (overrideFrameTime) {
            frameTime = time;
        }

        // Prepare channels
        int totalChannels = 0;
        foreach (BVHBone bone in boneList) {
            totalChannels += bone.channelNumber;
        }
        int channel = 0;
        channels = new float[totalChannels][];
        foreach (BVHBone bone in boneList) {
            for (int i = 0; i < bone.channelNumber; i++) {
                channels[channel] = new float[frames];
                bone.channels[bone.channelOrder[i]].values = channels[channel++];
            }
        }
        
        // Parse frames
        for (int i = 0; i < frames; i++) {
            newline();
            for (channel = 0; channel < totalChannels; channel++) {
                skipInLine();
                assure("channel value", getFloat(out channels[channel][i]));
            }
        }
    }

    public float[][] GetChannels()
    {
        return (float[][]) channels.Clone();
    }

    public BVHParser(string bvhText) {
        this.bvhText = bvhText;

        parse(false, 0f);
    }

    public BVHParser(string bvhText, float time) {
        this.bvhText = bvhText;

        parse(true, time);
    }
}
