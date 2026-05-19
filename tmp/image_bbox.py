from PIL import Image
import numpy as np

def stats(path: str) -> None:
    im = Image.open(path).convert("RGB")
    arr = np.array(im)
    bg = tuple(arr[0, 0].tolist())
    mask = np.any(arr != bg, axis=2)
    ys, xs = np.where(mask)
    if ys.size == 0:
        bbox = None
        bbox_w = None
        bbox_h = None
    else:
        bbox = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
        bbox_w = bbox[2] - bbox[0]
        bbox_h = bbox[3] - bbox[1]

    print(
        f"{path} size={im.size} bg={bg} bbox={bbox} bbox_w={bbox_w} bbox_h={bbox_h}"
    )

stats("assets/flowchart-product-sync.png")
stats("assets/flowchart-e2e.png")
