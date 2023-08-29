﻿/// <summary>
/// The indexer/searcher service.
/// </summary>
namespace bg3_modders_multitool.Services
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Linq;
    using Lucene.Net.Store;
    using Lucene.Net.Analysis;
    using Lucene.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Documents;
    using Lucene.Net.Search;
    using Lucene.Net.QueryParsers.Classic;
    using System.Threading.Tasks;
    using bg3_modders_multitool.ViewModels;
    using Lucene.Net.Analysis.Core;
    using Lucene.Net.Analysis.En;
    using Lucene.Net.Analysis.Util;
    using J2N;
    using Alphaleonis.Win32.Filesystem;

    public class IndexHelper
    {
        // images: .png, .DDS, .dds, .tga
        // models: .ttf, .gr2, .GR2, .gtp
        // audio: .wem
        // video: .bk2
        // shaders: .bshd, .shd
        private static readonly string[] extensionsToExclude = { ".png", ".dds", ".DDS", ".ttf", ".gr2", ".GR2", ".gtp", ".wem", ".bk2", ".ffxanim", ".tga", ".bshd", ".shd", ".jpg" };
        private static readonly string[] imageExtensions = { ".png", ".dds", ".DDS", ".tga", ".jpg" };
        public static readonly string[] BinaryExtensions = { ".lsf", ".bin", ".loca", ".data", ".patch" };
        private static readonly string luceneIndex = "lucene/index";
        public SearchResults DataContext;
        public string SearchText;
        private readonly FSDirectory fSDirectory;

        public IndexHelper()
        {
            fSDirectory = FSDirectory.Open(luceneIndex);
        }

        public void Clear()
        {
            fSDirectory.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #region Indexing
        /// <summary>
        /// Generates an index using the given filelist.
        /// </summary>
        /// <param name="filelist">The list of files to index.</param>
        public Task Index(List<string> filelist = null)
        {
            return Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() => {
                    DataContext.IsIndexing = true;
                });
                if (filelist==null)
                {
                    GeneralHelper.WriteToConsole(Properties.Resources.RetrievingFileList);
                    filelist = FileHelper.DirectorySearch(@"\\?\" + Path.GetFullPath("UnpackedData"));
                }

                // Display total file count being indexed
                GeneralHelper.WriteToConsole(Properties.Resources.FileListRetrieved);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DataContext.IndexFileTotal = filelist.Count;
                    DataContext.IndexStartTime = DateTime.Now;
                    DataContext.IndexFileCount = 0;
                });

                if (System.IO.Directory.Exists(luceneIndex))
                    System.IO.Directory.Delete(luceneIndex, true);
                IndexFiles(filelist, new CustomAnalyzer());
            });
        }

        /// <summary>
        /// Indexes the given files using an analyzer.
        /// </summary>
        /// <param name="files">The file list to index.</param>
        /// <param name="analyzer">The analyzer to use when indexing.</param>
        private void IndexFiles(List<string> files, Analyzer analyzer)
        {
            GeneralHelper.WriteToConsole(Properties.Resources.IndexingInProgress);
            using (Analyzer a = analyzer)
            {
                IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, a);
                using (IndexWriter writer = new IndexWriter(fSDirectory, config))
                {
                    Parallel.ForEach(files, GeneralHelper.ParallelOptions, file => {
                        try
                        {
                            IndexLuceneFile(file, writer);
                        }
                        catch (OutOfMemoryException)
                        {
                            GeneralHelper.WriteToConsole(Properties.Resources.OutOfMemFailedToIndex, file);
                        }
                    });
                    writer.Commit();
                    analyzer.Dispose();
                    writer.Dispose();
                }
            }
            GeneralHelper.WriteToConsole(Properties.Resources.IndexFinished, DataContext.GetTimeTaken().ToString("hh\\:mm\\:ss"));
            Application.Current.Dispatcher.Invoke(() => {
                DataContext.IsIndexing = false;
            });
        }

        /// <summary>
        /// Adds a file to the index.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="writer">The index to write to.</param>
        private void IndexLuceneFile(string file, IndexWriter writer)
        {
            try
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file);
                // if file type is excluded, only track file name and path so it can be searched for by name
                var contents = extensionsToExclude.Contains(extension) ? string.Empty : File.ReadAllText(file);
                file = file.Replace(@"\\?\", string.Empty).Replace(@"\\", @"\").Replace($"{System.IO.Directory.GetCurrentDirectory()}\\UnpackedData\\", string.Empty);
                var doc = new Document
                {
                    //new Int64Field("id", id, Field.Store.YES),
                    new TextField("path", file, Field.Store.YES),
                    new TextField("title", fileName, Field.Store.YES),
                    new TextField("body", contents, Field.Store.NO)
                };
                writer.AddDocument(doc);
            }
            catch(Exception ex)
            {
                GeneralHelper.WriteToConsole(Properties.Resources.FailedToIndexFile, file, ex.Message);
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DataContext.IndexFileCount++;
            });
        }
        #endregion

        #region Searching
        /// <summary>
        /// Determines whether a lucene index directory exists.
        /// </summary>
        /// <returns>Whether the index exists.</returns>
        public static bool IndexDirectoryExists()
        {
            return System.IO.Directory.Exists(luceneIndex) && System.IO.Directory.EnumerateFiles(luceneIndex).Any();
        }

        public static bool IsIndexCorrupt(FSDirectory fSDirectory)
        {
            return !new CheckIndex(fSDirectory).DoCheckIndex().Clean;
        }

        /// <summary>
        /// Searches for and displays results.
        /// </summary>
        /// <param name="search">The text to search for. Supports file title and contents.</param>
        /// <param name="writeToConsole">Whether or not to write search status to console (errors still report).</param>
        /// <param name="selectedFileTypes">The selected file types to filter on</param>
        /// <returns>The list of matches and the list of filtered matches</returns>
        public Task<(List<string> Matches,List<string>FilteredMatches)> SearchFiles(string search, bool writeToConsole = true, System.Collections.IList selectedFileTypes = null)
        {
            SearchText = search;
            return Task.Run(() => { 
                var matches = new List<string>();
                var filteredMatches = new List<string>();
                if (!IndexDirectoryExists() && !DirectoryReader.IndexExists(fSDirectory))
                {
                    GeneralHelper.WriteToConsole(Properties.Resources.IndexNotFound);
                    return (Matches: matches, FilteredMatches: filteredMatches);
                }

                try
                {
                    using (Analyzer analyzer = new CustomAnalyzer())
                    using (IndexReader reader = DirectoryReader.Open(fSDirectory))
                    {
                        IndexSearcher searcher = new IndexSearcher(reader);
                        MultiFieldQueryParser queryParser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, new[] { "title", "body" }, analyzer)
                        {
                            AllowLeadingWildcard = true
                        };
                        Query searchTermQuery = queryParser.Parse('*' + QueryParser.Escape(search.Trim()) + '*');

                        BooleanQuery aggregateQuery = new BooleanQuery() {
                            { searchTermQuery, Occur.MUST }
                        };

                        if (reader.MaxDoc != 0)
                        {
                            var start = DateTime.Now;
                            if(writeToConsole)
                                GeneralHelper.WriteToConsole(Properties.Resources.IndexSearchStarted);

                            // perform search
                            TopDocs topDocs = searcher.Search(aggregateQuery, reader.MaxDoc);

                            var filteredSomeResults = 0;
                            var missingExtensions = new List<string>();

                            // display results
                            foreach (ScoreDoc scoreDoc in topDocs.ScoreDocs)
                            {
                                float score = scoreDoc.Score;
                                int docId = scoreDoc.Doc;

                                Document doc = searcher.Doc(docId);
                                var path = doc.Get("path");
                                var ext = Path.GetExtension(path).ToLower();
                                ext = string.IsNullOrEmpty(ext) ? Properties.Resources.Extensionless : ext;
                                if (selectedFileTypes != null && !selectedFileTypes.Contains(ext)) // TODO - add option to turn this off in config
                                {
                                    filteredSomeResults++;
                                    if(!FileHelper.FileTypes.Contains(ext))
                                    {
                                        missingExtensions.Add(ext);
                                    }
                                    filteredMatches.Add(path);
                                    continue;
                                }

                                matches.Add(path);
                            }

                            if(missingExtensions.Count > 0)
                            {
                                GeneralHelper.WriteToConsole(Properties.Resources.MissingFileTypes, string.Join(",", missingExtensions.Distinct()));
                            }

                            if (writeToConsole)
                            {
                                GeneralHelper.WriteToConsole(Properties.Resources.IndexSearchReturned, matches.Count, TimeSpan.FromTicks(DateTime.Now.Subtract(start).Ticks).TotalMilliseconds);
                                if(filteredSomeResults > 0)
                                {
                                    GeneralHelper.WriteToConsole(Properties.Resources.ResultsHaveBeenFiltered, filteredSomeResults);
                                }
                            }
                        }
                        else
                        {
                            GeneralHelper.WriteToConsole(Properties.Resources.IndexSearchNoDocuments);
                        }
                    }
                }
                catch(Exception ex)
                {
                    // Checking if the index is corrupt is slower than just letting it fail
                    GeneralHelper.WriteToConsole($"{ex.Message}\n{ex.StackTrace}");
                }

                return (Matches: matches, FilteredMatches: filteredMatches);
            });
        }
        #endregion

        /// <summary>
        /// Gets a list of matching lines within a given file.
        /// </summary>
        /// <param name="path">The file path to read from.</param>
        /// <returns>A list of file line and trimmed contents.</returns>
        public Dictionary<int, string> GetFileContents(string path)
        {
            var lines = new Dictionary<int, string>();
            var lineCount = 1;
            if (File.Exists(path))
            {
                var extension = Path.GetExtension(path);
                var isExcluded = extensionsToExclude.Contains(extension);
                if (!isExcluded)
                {
                    using (var stream = File.OpenText(path))
                    using (System.IO.StreamReader r = stream)
                    {
                        string line;
                        var searchArray = SearchText.Split(' ');
                        while ((line = r.ReadLine()) != null)
                        {
                            var matched = false;
                            var escapedLine = line;
                            foreach(var searchText in searchArray)
                            {
                                if (line.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    if(!matched)
                                    {
                                        escapedLine = System.Security.SecurityElement.Escape(line);
                                        matched = true;
                                    }
                                    for (int index = 0; ; index += searchText.Length)
                                    {
                                        index = line.IndexOf(searchText, index, StringComparison.OrdinalIgnoreCase);
                                        if (index == -1)
                                            break;
                                        var text = System.Security.SecurityElement.Escape(line.Substring(index, searchText.Length));
                                        escapedLine = escapedLine.Replace(text, $"<Span Background=\"Yellow\">{text}</Span>");
                                    }
                                }
                            }
                            if(matched)
                            {
                                lines.Add(lineCount, escapedLine);
                            }
                            lineCount++;
                        }
                    }
                }
                if (lines.Count == 0)
                {
                    if(imageExtensions.Contains(extension))
                    {
                        lines.Add(0, $"<InlineUIContainer xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Image Source=\"{path.Replace("\\\\?\\", "")}\" Height=\"500\"></Image></InlineUIContainer>");
                    }
                    else
                    {
                        lines.Add(0, Properties.Resources.NoLinesFound);
                    }
                }
            }
            else
            {
                if (lines.Count == 0)
                {
                    lines.Add(0, Properties.Resources.FileNoExist);
                }
            }
            return lines;
        }
    }

    /// <summary>
    /// Custom analyzer for handling UUIDs. Forces lowercase and ignores common stop words
    /// </summary>
    public class CustomAnalyzer : Analyzer
    {
        protected override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
        {
            Tokenizer tokenizer = new CustomTokenizer(LuceneVersion.LUCENE_48, reader);
            TokenStream result = new LowerCaseFilter(LuceneVersion.LUCENE_48, tokenizer);
            result = new StopFilter(LuceneVersion.LUCENE_48, result, EnglishAnalyzer.DefaultStopSet);
            return new TokenStreamComponents(tokenizer, result);
        }
    }

    /// <summary>
    /// Custom tokenizer for handling UUIDs.
    /// </summary>
    public sealed class CustomTokenizer : CharTokenizer
    {
        private readonly int[] allowedSpecialCharacters = {'-','(',')','"','_','&',';','=','.',':','‘'};

        public CustomTokenizer(LuceneVersion matchVersion, System.IO.TextReader input) : base(matchVersion, input) { }

        /// <summary>
        /// Split tokens on non alphanumeric characters (excluding '-','(',')','"','_','&',';','=','.',':','‘')
        /// </summary>
        /// <param name="c">The character to compare</param>
        /// <returns>Whether the token should be split.</returns>
        protected override bool IsTokenChar(int c)
        {
            return Character.IsLetterOrDigit(c) || allowedSpecialCharacters.Contains(c);
        }
    }

}
