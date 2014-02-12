import os, subprocess
from blast_config import *

print('ncbi = %s' % ncbi)
print('blastout = %s' % blastout_root)

def run_blast(input_file_name):
    input_file_path = os.path.join(inputncbi, input_file_name)
    output_file_name = blast_output_file_template % input_file_name
    output_file_path = os.path.join(blastout_root, output_file_name)
    if not os.path.exists(input_file_path):
        print('input "%s" not found (%s)' % (input_file_name, input_file_path))
    else:
        subprocess.call([blast_exe, '-db', est_human, '-query', input_file_path, '-out', output_file_path])
        return output_file_path

