shader_type spatial;
render_mode cull_disabled;
instance uniform vec4 team_color : source_color = vec4(0.275, 0.761, 0.8, 1.0);
void fragment() {
    ALBEDO = mix(COLOR.rgb, team_color.rgb, UV.x);
    ROUGHNESS = 0.73;
    METALLIC = 0.15;
    EMISSION = team_color.rgb * UV.x * 0.55;
}
