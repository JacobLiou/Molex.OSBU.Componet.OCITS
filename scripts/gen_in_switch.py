# -*- coding: utf-8 -*-
import os

def msw_for_channel(c):
    # SW1 级联：1,1,2=绿灯→上路→模块9(IN1~8)；1,1,1=红灯→下路→模块10(IN9~16)
    if c <= 8:
        return "MSW 1,1,2;9,1,{};".format(c)
    return "MSW 1,1,1;10,1,{};".format(c - 8)

def build_block(product, channel):
    return "[{}::{}:16]\n{}\n".format(product, channel, msw_for_channel(channel))

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
in_path = os.path.join(root, "switch", "interleaverSwitch-MPLUS-IN")
ex_path = os.path.join(root, "doc", "switch", "ITL_MPLUS_SW_IN.example")

blocks = []
for p in range(1, 17):
    for c in range(1, 17):
        blocks.append(build_block(p, c).rstrip())

with open(in_path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n\n".join(blocks) + "\n")

ex_blocks = [build_block(1, c).rstrip() for c in range(1, 17)]

with open(ex_path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n\n".join(ex_blocks) + "\n")

print("Wrote", in_path)
print("Wrote", ex_path)
print("Sample [1::1:16]:", msw_for_channel(1))
print("Sample [1::9:16]:", msw_for_channel(9))
