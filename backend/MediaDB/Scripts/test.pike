#!/usr/local/bin/pike

string constr = "mysql://webuser:ResubeW@localhost/mediadb";
Sql.Sql db = Sql.Sql(constr);
mapping base_paths = ([]);

int main(int argc, array(string) argv)
{
  string sql = "SELECT * FROM base_path";
  array r = db->query(sql);

  foreach (r, mapping path)
    base_paths[(int)path->id] = path->path;

  string path;
  int base = 0;

  if (argc > 1) {
    sscanf(argv[1], "%d:%s", base, path);
  }

  if (path)
    path = normalize_path(path);

  build_tree(base, path);

  return 0;
}

string normalize_path(string p)
{
  if (!p || !sizeof(p))
    return 0;

  p = replace(p, "//", "/");
  if (p[-1] == '/')
    p = p[0..sizeof(p)-2];
  if (sizeof(p) && p[0] == '/')
    p = p[1..];

  return p;
}

string build_tree(int root, string path)
{
  string fullpath = base_paths[root];
  if (fullpath && path)
    fullpath += "/" + path;
  
  werror("$$$ Full path: %s\n", fullpath||"");
  
  foreach (base_paths; int id; string dir) {

    write("* %s\n", basename(dir));

    if (id == root && fullpath) {
      int ind = 1;
      foreach (((path||"")/"/")-({""}), string p)
      	write("%s* %s\n", " "*(ind++*2), p);
      
      if (fullpath) {
      	string pad = " "*(ind*2);
      	get_dirs(fullpath);
      	foreach (get_files(fullpath), mapping m) {
      	  write("%s1- %s\n", pad, m->name);
      	}
      }
    }
  }
}

array(string) get_dirs(string path)
{
  werror("Get dirs for: %s\n", path);
  string sql = "SELECT * FROM file WHERE dirname REGEXP '"+DB.Sql.quote(path)+"/.[^/]*$' "
               "GROUP BY dirname";
  array r = db->query(sql, path);
  werror("%O\n", r->dirname);
}

array(mapping) get_files(string path)
{
  string sql = "SELECT * FROM file WHERE dirname=%s";
  return db->query(sql, path);
}