﻿using System.Text;

namespace DocDiffTests;

internal abstract class Samples
{
    public const string ShortLeft = @"There now is your insular city of the Manhattoes, belted round by
wharves as Indian isles by coral reefs—commerce surrounds it with her
surf. Right and left, the streets take you waterward. Its extreme
downtown is the battery, where that noble mole is washed by waves, and
cooled by breezes, which a few hours previous were out of sight of
land. Look at the crowds of water-gazers there.";

    public const string ShortRight = @"Here now is your island city of the Manhatten, belted round by
wharves as Indian isles by coral reefs—commerce surrounds it with her
surf. Right and left, the streets take you to the sea. Its extreme
downtown is the battery, where that noble mole is washed by waves, and
cooled by breezes, which a few hours previous were out of sight of
land. Look at the crowds of water-gazers there.";


    public const string LongLeft = @"I stuffed a shirt or two into my old carpet-bag, tucked it under my
arm, and started for Cape Horn and the Pacific. Quitting the good city
of old Manhatto, I duly arrived in New Bedford. It was a Saturday night
in December. Much was I disappointed upon learning that the little
packet for Nantucket had already sailed, and that no way of reaching
that place would offer, till the following Monday.

As most young candidates for the pains and penalties of whaling stop at
this same New Bedford, thence to embark on their voyage, it may as well
be related that I, for one, had no idea of so doing. For my mind was
made up to sail in no other than a Nantucket craft, because there was a
fine, boisterous something about everything connected with that famous
old island, which amazingly pleased me. Besides though New Bedford has
of late been gradually monopolising the business of whaling, and though
in this matter poor old Nantucket is now much behind her, yet Nantucket
was her great original—the Tyre of this Carthage;—the place where the
first dead American whale was stranded. Where else but from Nantucket
did those aboriginal whalemen, the Red-Men, first sally out in canoes
to give chase to the Leviathan? And where but from Nantucket, too, did
that first adventurous little sloop put forth, partly laden with
imported cobblestones—so goes the story—to throw at the whales, in
order to discover when they were nigh enough to risk a harpoon from the
bowsprit?

Now having a night, a day, and still another night following before me
in New Bedford, ere I could embark for my destined port, it became a
matter of concernment where I was to eat and sleep meanwhile. It was a
very dubious-looking, nay, a very dark and dismal night, bitingly cold
and cheerless. I knew no one in the place. With anxious grapnels I had
sounded my pocket, and only brought up a few pieces of silver,—So,
wherever you go, Ishmael, said I to myself, as I stood in the middle of
a dreary street shouldering my bag, and comparing the gloom towards the
north with the darkness towards the south—wherever in your wisdom you
may conclude to lodge for the night, my dear Ishmael, be sure to
inquire the price, and don’t be too particular.

With halting steps I paced the streets, and passed the sign of “The
Crossed Harpoons”—but it looked too expensive and jolly there. Further
on, from the bright red windows of the “Sword-Fish Inn,” there came
such fervent rays, that it seemed to have melted the packed snow and
ice from before the house, for everywhere else the congealed frost lay
ten inches thick in a hard, asphaltic pavement,—rather weary for me,
when I struck my foot against the flinty projections, because from
hard, remorseless service the soles of my boots were in a most
miserable plight. Too expensive and jolly, again thought I, pausing one
moment to watch the broad glare in the street, and hear the sounds of
the tinkling glasses within. But go on, Ishmael, said I at last; don’t
you hear? get away from before the door; your patched boots are
stopping the way. So on I went. I now by instinct followed the streets
that took me waterward, for there, doubtless, were the cheapest, if not
the cheeriest inns.

Such dreary streets! blocks of blackness, not houses, on either hand,
and here and there a candle, like a candle moving about in a tomb. At
this hour of the night, of the last day of the week, that quarter of
the town proved all but deserted. But presently I came to a smoky light
proceeding from a low, wide building, the door of which stood
invitingly open. It had a careless look, as if it were meant for the
uses of the public; so, entering, the first thing I did was to stumble
over an ash-box in the porch. Ha! thought I, ha, as the flying
particles almost choked me, are these ashes from that destroyed city,
Gomorrah? But “The Crossed Harpoons,” and “The Sword-Fish?”—this, then
must needs be the sign of “The Trap.” However, I picked myself up and
hearing a loud voice within, pushed on and opened a second, interior
door.";
    
    public const string LongRight = 
        @"I put two shirts into my old home-made bag, tucked it under my
arm, and started for the Tierra del Fuego and the Pacific.
Quitting the good city of Manhattan, I duly arrived in New Bedford
on a Saturday night in December.
Much was I disappointed upon learning that the little boat for Nantucket
had already sailed, and there was no way there until the following Monday.

As most young candidates for the difficulties of whaling stop here,
thence to embark on their voyage, it may as well be related that I,
for one, had no idea of so doing. For my mind was made up to sail in no
other than a Nantucket craft, because there was a fine, boisterous
something about everything connected with that famous old island, which
amazingly pleased me. Besides though New Bedford has of late been 
monopolising the business of whaling, and though in this matter poor
old Nantucket is now much behind her, yet Nantucket was the original,
the place where the first dead American whale was stranded.
Where else but from Nantucket did those aboriginal whalemen, first go
in canoes to give chase to the Leviathan? And where but from Nantucket,
too, did that first adventurous little sloop put forth, partly laden with
imported cobblestones—so goes the story—to throw at the whales, in
order to discover when they were nigh enough to risk a harpoon from the
bowsprit?

Now having a night, a day, and still another night following before me
in New Bedford, ere I could embark for my destined port, it became a
matter of concernment where I was to eat and sleep meanwhile. It was a
very dubious-looking, nay, a very dark and dismal night, bitingly cold
and cheerless. I knew no one in the place. With anxious grapnels I had
sounded my pocket, and only brought up a few pieces of silver,—So,
wherever you go, Ishmael, said I to myself, as I stood in the middle of
a dreary street shouldering my bag, and comparing the gloom towards the
north with the darkness towards the south—wherever in your wisdom you
may conclude to lodge for the night, my dear Ishmael, be sure to
inquire the price, and don’t be too particular.

With halting steps I paced the streets, and passed the sign of 'The
Crossed Harpoons', but it looked too expensive and jolly there. Further
on, from the bright red windows of the 'Sword-Fish Inn,' there came
such fervent rays, that it seemed to have melted the packed snow and
ice from before the house, for everywhere else the congealed frost lay
ten inches thick in a hard, asphaltic pavement, rather weary for me,
when I struck my foot against the flinty projections, because from
hard, remorseless service the soles of my boots were in a most
miserable plight. Too expensive and jolly, again thought I, pausing one
moment to watch the broad glare in the street, and hear the sounds of
the tinkling glasses within. But go on, Ishmael, said I at last; don’t
you hear? get away from before the door; your patched boots are
stopping the way. So on I went. I now by instinct followed the streets
that took me waterward, for there, doubtless, were the cheapest, if not
the cheeriest inns.

Such dreary streets! Not houses, but blocks of blackness on either hand.
And here and there a candle, like a candle moving about in a tomb. At
this hour of the night, of the last day of the week, that quarter of
the town proved all but deserted. But presently I came to a smoky light
proceeding from a low, wide building, the door of which stood
invitingly open. It had a careless look, as if it were meant for the
uses of the public; so, entering, the first thing I did was to stumble
over an ash-box in the porch. 'Ha!' I thought, 'ha!', as the flying
particles almost choked me, are these ashes from that destroyed city,
Gomorrah? But 'The Crossed Harpoons' and 'The Sword-Fish' — this, then
must needs be the sign of 'The Trap'. However, I picked myself up and
hearing a loud voice within, pushed on and opened a second, interior
door.";


    public static string ComplexLeft {
        get
        {
            var sb = new StringBuilder();
            for (int i = 32; i < 255; i++)
            {
                if (i >= 0x7F) sb.Append((char)(i + 33));
                else sb.Append((char)i);
            }

            return sb.ToString();
        }
    }

    public static string ComplexRight {
        get {
            var sb = new StringBuilder();
            for (int i = 32; i < 255; i++)
            {
                if ((i % 9) == 0) sb.Append("^!");
                else if ((i % 13) == 0) sb.Append("x");
                else
                {
                    if (i >= 0x7F) sb.Append((char)(i + 33));
                    else sb.Append((char)i);
                }
            }

            return sb.ToString();
        }
    }
}