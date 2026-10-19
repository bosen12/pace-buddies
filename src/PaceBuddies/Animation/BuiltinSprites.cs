using System.Drawing;
using System.Drawing.Imaging;

namespace PaceBuddies.Animation;

// Original small pixel characters. Drawn as code so source, previews and tray art stay editable.
public static class BuiltinSprites
{
    public static AnimationAsset Create(string kind)
    {
        var frames=new List<Bitmap>();
        for(int phase=0;phase<8;phase++) frames.Add(Draw(kind,phase));
        return new(frames,Enumerable.Repeat(100,8).ToArray(),"原創像素 · 32 × 32 · 8 格",true);
    }
    private static Bitmap Draw(string kind,int phase)
    {
        var b=new Bitmap(32,32,PixelFormat.Format32bppArgb);
        using var g=Graphics.FromImage(b);
        var bounce=new[]{0,-1,-2,-1,0,1,0,-1}[phase];
        void Box(string color,int x,int y,int w,int h) { using var brush=new SolidBrush(ColorTranslator.FromHtml(color)); g.FillRectangle(brush,x,y+bounce,w,h); }
        const string ink="#263650",light="#FFF9ED";
        if(kind=="blob")
        {
            Box(ink,7,10,18,15); Box(ink,5,14,22,8); Box(ink,10,8,12,18);
            Box("#F3BA68",8,11,16,12); Box("#F3BA68",6,15,20,5); Box("#FFD98A",11,9,10,5);
            Box("#D9954D",8,21,16,3); Box(ink,11,16,2,3); Box(ink,20,16,2,3);
            Box("#F58E82",8,19,3,2); Box("#F58E82",23,19,2,2); Box(light,11,11,3,2);
            Box(ink,9+(phase%2),25,5,2); Box(ink,20-(phase%2),25,5,2);
            Box("#86B9AD",16,5,2,4); Box("#86B9AD",18,4,4,2);
        }
        else if(kind=="bird")
        {
            Box(ink,10,9,15,15); Box(ink,8,12,19,9); Box("#7DBBCE",11,10,13,13); Box("#A9DBE5",10,13,16,6);
            Box(light,17,16,7,7); Box(ink,21,12,2,3); Box("#F5B677",25,16,5,3);
            Box(ink,4,17+phase%3,9,3); Box("#5A99B7",6,16+phase%3,8,3);
            Box("#3976B9",12,16-phase%3,6,5); Box("#F5B677",13,24,2,3); Box("#F5B677",21,24,2,3);
            Box(ink,14,7,3,3); Box("#7DBBCE",14,8,2,3);
        }
        else
        {
            Box(ink,9,12,14,11); Box(ink,18,7,10,13); Box(ink,18,4,3,6); Box(ink,25,4,3,6);
            Box(light,10,13,12,9); Box(light,19,8,8,10); Box(light,19,5,1,4); Box(light,26,5,1,4);
            Box("#EAC7AB",20,9,3,3); Box(ink,25,11,2,2); Box("#E8978B",26,15,2,1);
            Box(ink,5,11,5,3); Box(ink,3,8,3,5); Box(light,5,10,4,2);
            var stride=new[]{-2,-1,0,1,2,1,0,-1}[phase];
            Box(ink,10+stride,22,3,4); Box(light,10+stride,22,2,3);
            Box(ink,20-stride,22,3,4); Box(light,20-stride,22,2,3);
            Box("#F5B677",16,13,3,7); Box("#D29558",16,16,3,2);
        }
        return b;
    }
}
