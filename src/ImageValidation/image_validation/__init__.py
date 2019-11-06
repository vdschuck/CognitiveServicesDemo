import os
import main

files = []
dataset_path = 'c:\\projects\\git\\CognitiveServicesDemo\\images\\dataset\\'
folders = ['first_test', 'second_test', 'third_test', 'fourth_test', 'fifth_test', 'sixth_test', 'seventh_test', 'eighth_test']

for folder in folders:
    path = dataset_path + folder

    # r=root, d=directories, f = files
    for r, d, f in os.walk(path):
        for file in f:
            if '.jpg' in file:
                files.append(os.path.join(r, file))

main.handler_validation(files)