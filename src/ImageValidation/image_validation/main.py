import cpbd
from scipy import ndimage

def handler_validation(files):
    count = 0

    for item in files:
        input_image = ndimage.imread(item, mode='L')
        result = cpbd.compute(input_image)
        score = float("{0:.4f}".format(result))

        if (score * 100) < 50:
            print('Path %s: %f' % (item, score))
            count += 1

    print('Total images below score: %i' % (count))