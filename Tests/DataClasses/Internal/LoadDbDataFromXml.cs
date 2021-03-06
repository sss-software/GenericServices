﻿#region licence
// The MIT License (MIT)
// 
// Filename: LoadDbDataFromXml.cs
// Date Created: 2014/05/26
// 
// Copyright (c) 2014 Jon Smith (www.selectiveanalytics.com & www.thereformedprogrammer.net)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Tests.DataClasses.Concrete;

[assembly: InternalsVisibleTo("Tests")]

namespace Tests.DataClasses.Internal
{

    internal class LoadDbDataFromXml
    {
        private Dictionary<string, Tag> _tagsDict;


        public List<Blog> Bloggers { get; private set; }

        public List<Post> Posts { get; private set; }

        public IEnumerable<Tag> Tags { get { return _tagsDict.Values; } }

        public List<PostTagGrade> PostTagGrades { get; private set; }

        //ctor
        public LoadDbDataFromXml(string fullFilepath)
        {
            //var assemblyHoldingFile = Assembly.GetExecutingAssembly();

            //using (var fileStream = assemblyHoldingFile.GetManifestResourceStream(fullFilepath))
            using (var fileStream = new XmlTextReader(fullFilepath))
            {
                var xmlData = XElement.Load(fileStream);

                //now decode and return
                _tagsDict = DecodeTags(xmlData.Element("Tags"));
                DecodeBlogsAndGrades(xmlData.Element("Blogs"), _tagsDict);
            }
        }

        private void DecodeBlogsAndGrades(XElement element, Dictionary<string, Tag> tagsDict)
        {
            Bloggers = new List<Blog>();
            Posts = new List<Post>();
            PostTagGrades = new List<PostTagGrade>();
            foreach (var blogXml in element.Elements("Blog"))
            {
                var newBlogger = new Blog()
                {
                    Name = blogXml.Element("Name").Value,
                    EmailAddress = blogXml.Element("Email").Value,
                    Posts = new Collection<Post>()
                };

                foreach (var postXml in blogXml.Element("Posts").Elements("Post"))
                {
                    var newPost = new Post()
                    {
                        Blogger = newBlogger,
                        Title = postXml.Element("Title").Value,
                        Content = postXml.Element("Content").Value,
                        Tags = postXml.Element("TagSlugs").Value.Split(',').Select(x => tagsDict[x.Trim()]).ToList()
                    };


                    //look for PostTagGrades for this post
                    foreach (var postTagXml in postXml.Elements("PostTagGrade"))
                    {
                        var newPostTag = new PostTagGrade
                        {
                            PostPart = newPost,
                            TagPart = tagsDict[postTagXml.Element("TagSlug").Value.Trim()],
                            Grade = int.Parse( postTagXml.Element("Grade").Value)
                        };
                        PostTagGrades.Add( newPostTag);
                    }
                    Posts.Add(newPost);
                    newBlogger.Posts.Add(newPost);

                }
                Bloggers.Add(newBlogger);
            }
        }

        private static Dictionary<string, Tag> DecodeTags(XElement element)
        {
            var result = new Dictionary<string, Tag>();
            foreach (var newTag in element.Elements("Tag").Select(tagXml => new Tag()
            {
                Name = tagXml.Element("Name").Value,
                Slug = tagXml.Element("Slug").Value
            }))
            {
                result[newTag.Slug] = newTag;
            }
            return result;
        }
    }
}
