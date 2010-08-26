#!/usr/bin/env pike

string constr = "mysql://webuser:ResubeW@snoop/mediadb";
Sql.Sql db = Sql.Sql(constr);
mapping base_paths = ([]);

int main(int argc, array(string) argv)
{
  string sql = "SELECT * FROM base_path";
  array r = db->query(sql);

  foreach (r, mapping path)
    base_paths[(int)path->id] = path->path;

  int dir_id = 0;

  if (argc > 1)
    dir_id = (int)argv[1];

  array a = ({});
  a = fetch_tree(dir_id, a);
  
  werror("%O\n", reverse(a));
  //build_tree(dir_id);

  return 0;
}

void build_tree(int id)
{
  foreach (base_paths; int dir_id; string path) {
    write("* %s\n", basename(path));
    if (id == dir_id) 
      low_build_tree(id, 1);
  }
}

void low_build_tree(int id, int indent)
{
  string pad = " "*(indent*2);
  write("%s* Open...\n", pad);
  array a = ({});
  a = fetch_tree(id, a);
  
  write("%O\n", a);
}

array fetch_tree(int id, array paths)
{
  string sql = "SELECT * FROM `directory` WHERE id=%d";
  array r = db->query(sql, id);
  if (r && sizeof(r)) {
    paths += ({ r[0] });
    if ((int)r[0]->parent_id > 0)
      return fetch_tree((int)r[0]->parent_id, paths);
  }

  return paths;
}
