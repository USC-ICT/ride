using System;

using Links = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.Link>;  //typedef  vector<Link*> Links;


namespace BllipParser.DotNet.Vanilla
{
    //typedef  vector<Link*> Links;
    //typedef  Links::iterator LinksIter;


    class Link
    {
        const int DUMMYVAL = 999;


        short key_;
        Links links_ = new Links();


        public Link(short key)
        {
            key_ = key;
        }

        //~Link()
        //{
        //    LinksIter li = links_.begin();
        //    for( ; li != links_.end() ; li++) delete (*li);
        //}


        public Link is_unique(InputTree tree, out bool ans, ref int cnt)
        {
            //cerr << "IU " << *tree << endl;
            Link nlink;
            Term trm = Term.get(tree.term());
            nlink = do_link(trm.toInt(), out ans);
            if (trm.terminal_p() != 0)
            {
                cnt++;
                return nlink;
            }

            var iti = tree.subTrees().First;  //InputTreesIter iti = tree->subTrees().begin();
            for ( ; iti != null; iti = iti.Next)  //for( ; iti != tree->subTrees().end() ; iti++)
            {
                nlink = nlink.is_unique(iti.Value, out ans, ref cnt);  //nlink = nlink->is_unique((*iti), ans,cnt);
            }

            nlink = nlink.do_link(DUMMYVAL, out ans);
            return nlink;
        }


        short key() { return key_; }


        Link do_link(int tint, out bool ans)
        {
            var li = links_.GetList().GetEnumerator();  //LinksIter li = links_.begin();
            for (bool liFound = li.MoveNext(); liFound; liFound = li.MoveNext())  //for( ; li != links_.end() ; li++)
            {
                Link slink = li.Current;  //Link* slink = (*li);
                if (slink.key() == tint)
                {
                    ans = false;
                    return slink;
                }
            }
            li.Dispose();

            ans = true;
            Link nlink = new Link((short)tint);
            //cerr << "LN " << tint << endl;
            links_.push_back(nlink);
            return nlink;
        }
    }
}
