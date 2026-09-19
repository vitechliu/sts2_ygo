"""只读导入大师决斗独立特效，生成当前项目的预览资源。"""
import argparse
from pathlib import Path
from md_effect_import import import_all

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',required=True,type=Path,help='Effects 源目录，只读')
    import_all(parser.parse_args().source.resolve())
