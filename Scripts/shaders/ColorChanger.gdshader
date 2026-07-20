shader_type canvas_item;

uniform float Red : hint_range(0.0,1.0,0.05);
uniform float Green : hint_range(0.0,1.0,0.05);
uniform float Blue : hint_range(0.0,1.0,0.05);
uniform float Alpha : hint_range(0.0,1.0,0.05);


void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	if (COLOR.r == 1.0){
		COLOR = vec4(Red,Green,Blue,Alpha);
	}
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
