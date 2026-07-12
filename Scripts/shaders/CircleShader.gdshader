shader_type canvas_item;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	if (distance(UV,vec2(0.5,0.5)) > 0.5){
		COLOR.a = 0.0;
	}
	// Called for every pixel the material is visible on.
}
