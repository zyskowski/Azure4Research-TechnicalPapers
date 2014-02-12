import os, subprocess

database_root_path = "/home/azureuser/BLAST/blastdb"
ncbi = os.path.join(database_root_path, "ncbi")
est_human = os.path.join(ncbi, "est_human")
inputncbi = os.path.join(database_root_path, "inputncbi")
blastout_root = os.path.join(database_root_path, "../blastout")
blast_exe = "/home/azureuser/ncbi-blast-2.2.28+/bin/blastn"
blast_output_file_template = "%s.out"
blast_input_file_template = "input_%s"


